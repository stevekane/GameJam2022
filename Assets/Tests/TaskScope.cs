using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public delegate Task TaskFunc(TaskScope scope);
public delegate Task<T> TaskFunc<T>(TaskScope scope);

public class TaskScope : IDisposable {
  CancellationTokenSource Source;

  public TaskScope() => Source = new();
  public TaskScope(TaskScope parent) => Source = parent.NewChildToken();

  public void Cancel() => Source.Cancel();
  public void Dispose() {
    Source.Cancel();
    Source.Dispose();
  }

  // Note: Only tasks invoked via Run() and RunChild() will be waited upon here. E.g. Any() will not.
  List<Task> JoinTasks = new();
  public Task Join => Task.Run(() => Task.WhenAll(JoinTasks), Source.Token);
  T AddTask<T>(T task) where T : Task {
    Source.Token.ThrowIfCancellationRequested();
    JoinTasks.Add(task);
    return task;
  }

  // Child task spawning.
  CancellationTokenSource NewChildToken() => CancellationTokenSource.CreateLinkedTokenSource(Source.Token);
  public Task Run(TaskFunc f) => AddTask(f(this));
  public Task<T> Run<T>(TaskFunc<T> f) => AddTask(f(this));
  public Task RunChild(out TaskScope childScope, TaskFunc f) {
    Source.Token.ThrowIfCancellationRequested();
    childScope = new(this);
    var scope = childScope;
    return AddTask(f(scope));
  }

  // Basic control flow.
  public async Task Yield() {
    Source.Token.ThrowIfCancellationRequested();
    await Task.Yield();
  }
  public Task Tick() => ListenFor(Timeval.TickEvent);
  public async Task Ticks(int ticks) {
    // Maybe this should be Repeat(ticks, Tick()) ?
    // Cons: more allocations that way.
    // Pros: fewer yields, and it guarantees to finish *after* N calls to FixedUpdate, whereas this
    // version will terminate at any point in the frame.
    int endTick = Timeval.TickCount + ticks;
    while (Timeval.TickCount < endTick)
      await Yield();
  }
  public Task Millis(int ms) => Task.Delay(ms, Source.Token);
  public Task Delay(Timeval t) => Millis((int)t.Millis);

  // Conditional control flow.
  public async Task While(Func<bool> pred) {
    while (pred())
      await Yield();
  }
  public async Task Until(Func<bool> pred) {
    while (!pred())
      await Yield();
  }
  public async Task Repeat(TaskFunc f) {
    while (true) {
      await f(this);
    }
  }
  public async Task Repeat(int n, TaskFunc f) {
    for (int i = 0; i < n; i++) {
      await f(this);
    }
  }

  // More involved combinators.
  public async Task<int> Any(params TaskFunc[] fs) {
    Source.Token.ThrowIfCancellationRequested();
    using TaskScope scope = new(this);
    try {
      var tasks = fs.Select(f => f(scope)).ToArray();
      var which = await Task.WhenAny(tasks);
      return tasks.IndexOf(which);
    } finally {
      Source.Token.ThrowIfCancellationRequested();
    }
  }
  public async Task All(params TaskFunc[] fs) {
    Source.Token.ThrowIfCancellationRequested();
    using TaskScope scope = new(this);
    try {
      await Task.WhenAll(fs.Select(f => f(scope)));
    } finally {
      Source.Token.ThrowIfCancellationRequested();
    }
  }
  public async Task ListenFor(IEventSource evt) {
    Source.Token.ThrowIfCancellationRequested();
    var done = NewChildToken();
    void callback() { done.Cancel(); }
    try {
      evt.Listen(callback);
      await Task.Delay(-1, done.Token);
    } catch {
    } finally {
      evt.Unlisten(callback);
      Source.Token.ThrowIfCancellationRequested();
    }
  }
  public async Task<T> ListenFor<T>(IEventSource<T> evt) {
    Source.Token.ThrowIfCancellationRequested();
    var done = NewChildToken();
    T eventResult = default;
    void callback(T value) { eventResult = value; done.Cancel(); }
    try {
      evt.Listen(callback);
      await Task.Delay(-1, done.Token);
      throw new Exception(); // not reached
    } catch {
      return eventResult;
    } finally {
      evt.Unlisten(callback);
      Source.Token.ThrowIfCancellationRequested();
    }
  }
  public async Task<int> ListenForAll<T>(IEventSource<T> evt, T[] results) {
    Source.Token.ThrowIfCancellationRequested();
    int resultCount = 0;
    void callback(T value) { results[resultCount++] = value; }
    try {
      evt.Listen(callback);
      await Tick();
      return resultCount;
    } finally {
      evt.Unlisten(callback);
      Source.Token.ThrowIfCancellationRequested();
    }
  }
}

public class TaskExtensionsForOlSteve {
  public static TaskFunc While(Func<bool> pred) => s => s.While(pred);
  public static TaskFunc Until(Func<bool> pred) => s => s.Until(pred);
  public static TaskFunc Repeat(TaskFunc f) => s => s.Repeat(f);
  public static TaskFunc Repeat(int n, TaskFunc f) => s => s.Repeat(n, f);
  public static TaskFunc ListenFor(IEventSource evt) => s => s.ListenFor(evt);
  public static TaskFunc<T> ListenFor<T>(IEventSource<T> evt) => s => s.ListenFor(evt);
  public static TaskFunc<int> ListenForAll<T>(IEventSource<T> evt, T[] results) => s => s.ListenForAll(evt, results);
}