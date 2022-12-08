using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
  public void ThrowIfCancelled() => Source.Token.ThrowIfCancellationRequested();

  // Child task spawning.
  CancellationTokenSource NewChildToken() => CancellationTokenSource.CreateLinkedTokenSource(Source.Token);
  public Task Run(TaskFunc f) => CheckTask(f(this));
  public Task<T> Run<T>(TaskFunc<T> f) => CheckTask(f(this));
  public Task RunChild(out TaskScope childScope, TaskFunc f) {
    ThrowIfCancelled();
    childScope = new(this);
    var scope = childScope;
    return CheckTask(f(scope));
  }
  T CheckTask<T>(T task) where T : Task {
    ThrowIfCancelled();
    return task;
  }

  // Fiber adapter.
  public async Task RunFiber(IEnumerator routine) {
    ThrowIfCancelled();
    Fiber f = routine as Fiber ?? new Fiber(routine);
    try {
      while (f.MoveNext())
        await Tick();
    } catch {
      f.Stop();
    }
  }

  // Basic control flow.
  public async Task Yield() {
    ThrowIfCancelled();
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
  public async Task<int> Any(params Task[] tasks) {
    ThrowIfCancelled();
    var which = await Task.WhenAny(tasks);
    return tasks.IndexOf(which);
  }
  public async Task<int> Any(params TaskFunc[] fs) {
    ThrowIfCancelled();
    using TaskScope scope = new(this);
    try {
      var tasks = fs.Select(f => f(scope)).ToArray();
      var which = await Task.WhenAny(tasks);
      return tasks.IndexOf(which);
    } finally {
      ThrowIfCancelled();
    }
  }
  public async Task All(params Task[] tasks) {
    ThrowIfCancelled();
    await Task.WhenAll(tasks);
  }
  public async Task All(params TaskFunc[] fs) {
    ThrowIfCancelled();
    using TaskScope scope = new(this);
    try {
      await Task.WhenAll(fs.Select(f => f(scope)));
    } finally {
      ThrowIfCancelled();
    }
  }
  public async Task ListenFor(IEventSource evt) {
    ThrowIfCancelled();
    var done = NewChildToken();
    void callback() { done.Cancel(); }
    try {
      evt.Listen(callback);
      await Task.Delay(-1, done.Token);
    } catch {
    } finally {
      evt.Unlisten(callback);
      ThrowIfCancelled();
    }
  }
  public async Task<T> ListenFor<T>(IEventSource<T> evt) {
    ThrowIfCancelled();
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
      ThrowIfCancelled();
    }
  }
  public async Task<int> ListenForAll<T>(IEventSource<T> evt, T[] results) {
    ThrowIfCancelled();
    var done = NewChildToken();
    int resultCount = 0;
    async void callback(T value) { results[resultCount++] = value; await Tick(); done.Cancel(); }
    try {
      evt.Listen(callback);
      await Task.Delay(-1, done.Token);
      throw new Exception(); // not reached
    } catch {
      return resultCount;
    } finally {
      evt.Unlisten(callback);
      ThrowIfCancelled();
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