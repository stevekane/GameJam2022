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
  public static TaskScope StartNew(TaskFunc f) {
    var scope = new TaskScope();
    scope.Start(f);
    return scope;
  }

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
  public Task RunChild(TaskFunc f) => RunChild(out var _, f);
  T CheckTask<T>(T task) where T : Task {
    ThrowIfCancelled();
    return task;
  }
  public void Start(TaskFunc f) {
    ThrowIfCancelled();
    TaskScope scope = new(this);
    var task = new Task(async () => {
      try {
        await f(scope);
      } catch (OperationCanceledException) {
      } finally {
        scope.Dispose();
      }
    });
    task.Start(TaskScheduler.FromCurrentSynchronizationContext());
  }
  // Fiber adapter.
  public async Task RunFiber(IEnumerator routine) {
    ThrowIfCancelled();
    try {
      while (routine.MoveNext())
        await Tick();
    } finally {
    }
  }

  // Basic control flow.
  [Obsolete("You probably want to use Tick instead. This is unreliable for time-sensitive use cases.")]
  public async Task Yield() {
    ThrowIfCancelled();
    await Task.Yield();
    ThrowIfCancelled();
  }
  public Task Tick() => ListenFor(Timeval.TickEvent);
  public async Task Ticks(int ticks) {
    for (int i = 0; i < ticks; i++)
      await Tick();
  }
  public Task Seconds(float seconds) => Task.Delay((int)(seconds * 1000), Source.Token);
  public Task Millis(int ms) => Task.Delay(ms, Source.Token);
  public Task Forever() => Task.Delay(-1, Source.Token);
  public Task Delay(Timeval t) => Millis((int)t.Millis);

  // Conditional control flow.
  public async Task While(Func<bool> pred) {
    while (pred())
      await Tick();
  }
  public async Task Until(Func<bool> pred) {
    while (!pred())
      await Tick();
  }
  public async Task Repeat(Action f) {
    while (true) {
      f();
      await Tick();
    }
  }
  public async Task Repeat(int n, Action f) {
    for (int i = 0; i < n; i++) {
      f();
      await Tick();
    }
  }
  public async Task Repeat(TaskFunc f) {
    while (true) {
      ThrowIfCancelled();
      await f(this);
      await Tick();
    }
  }
  public async Task Repeat(int n, TaskFunc f) {
    for (int i = 0; i < n; i++) {
      ThrowIfCancelled();
      await f(this);
      await Tick();
    }
  }

  // More involved combinators.
  public async Task<T> Return<T>(Task f, T result = default) {
    ThrowIfCancelled();
    await f;
    return result;
  }
  public async Task<int> AnyTask(params Task[] tasks) {
    ThrowIfCancelled();
    var which = await Task.WhenAny(tasks);
    if (which.IsFaulted)
      throw which.Exception;
    return tasks.IndexOf(which);
  }
  public async Task<int> Any(params TaskFunc[] fs) {
    ThrowIfCancelled();
    using TaskScope scope = new(this);
    try {
      var tasks = fs.Select(f => f(scope)).ToArray();
      var which = await Task.WhenAny(tasks);
      if (which.IsFaulted)
        throw which.Exception;
      return tasks.IndexOf(which);
    } finally {
      ThrowIfCancelled();
    }
  }
  public async Task<T> Any<T>(params TaskFunc<T>[] fs) {
    ThrowIfCancelled();
    using TaskScope scope = new(this);
    try {
      var tasks = fs.Select(f => f(scope)).ToArray();
      var which = await Task.WhenAny(tasks);
      if (which.IsFaulted)
        throw which.Exception;
      return await which;
    } finally {
      ThrowIfCancelled();
    }
  }
  public async Task AllTask(params Task[] tasks) {
    ThrowIfCancelled();
    using TaskScope scope = new(this);
    await Task.WhenAll(tasks.Select(t => AllInner(scope, async s => await t)));
  }
  public async Task All(params TaskFunc[] fs) {
    ThrowIfCancelled();
    using TaskScope scope = new(this);
    try {
      await Task.WhenAll(fs.Select(f => AllInner(scope, f)));
    } finally {
      ThrowIfCancelled();
    }
  }
  async Task AllInner(TaskScope scope, TaskFunc f) {
    try {
      await f(scope);
    } catch (OperationCanceledException) {
    } catch (Exception e) {
      scope.Cancel();
      throw e;
    }
  }
  public async Task ListenFor(IEventSource evt) {
    ThrowIfCancelled();
    var done = NewChildToken();
    void callback() { done.Cancel(); }
    try {
      evt.Listen(callback);
      await Task.Delay(-1, done.Token);
    } catch (OperationCanceledException) {
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
    } catch (OperationCanceledException) {
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
    async void callback(T value) { try { results[resultCount++] = value; await Tick(); } catch { } done.Cancel(); }
    try {
      evt.Listen(callback);
      await Task.Delay(-1, done.Token);
      throw new Exception(); // not reached
    } catch (OperationCanceledException) {
      return resultCount;
    } finally {
      evt.Unlisten(callback);
      ThrowIfCancelled();
    }
  }
}

public static class Waiter {
  public static TaskFunc Millis(int ms) => s => s.Millis(ms);
  public static TaskFunc Forever() => s => s.Forever();
  public static TaskFunc Delay(Timeval t) => s => s.Delay(t);
  public static TaskFunc While(Func<bool> pred) => s => s.While(pred);
  public static TaskFunc Until(Func<bool> pred) => s => s.Until(pred);
  public static TaskFunc Repeat(Action f) => s => s.Repeat(f);
  public static TaskFunc Repeat<T>(Action<T> f, T arg) => s => s.Repeat(() => f(arg));
  public static TaskFunc Repeat(TaskFunc f) => s => s.Repeat(f);
  public static TaskFunc Repeat(int n, TaskFunc f) => s => s.Repeat(n, f);
  public static TaskFunc Any(params TaskFunc[] fs) => s => s.Any(fs);
  public static TaskFunc All(params TaskFunc[] fs) => s => s.All(fs);
  public static TaskFunc ListenFor(IEventSource evt) => s => s.ListenFor(evt);
  public static TaskFunc<T> ListenFor<T>(IEventSource<T> evt) => s => s.ListenFor(evt);
  public static TaskFunc<int> ListenForAll<T>(IEventSource<T> evt, T[] results) => s => s.ListenForAll(evt, results);
  public static TaskFunc<T> Return<T>(TaskFunc f, T t = default) => s => s.Return<T>(f(s), t);
  public static TaskFunc Sequence(params TaskFunc[] fs) => async (TaskScope s) => {
    foreach (var f in fs) {
      await s.Run(f);
    }
  };
}