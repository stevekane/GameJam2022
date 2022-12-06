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
  public TaskScope(TaskScope parent) => Source = CancellationTokenSource.CreateLinkedTokenSource(parent.Source.Token);

  public void Cancel() => Source.Cancel();
  public void Dispose() {
    Source.Cancel();
    Source.Dispose();
  }

  // Note: Only tasks invoked via Run() and RunChild() will be waited upon here. E.g. Any() will not.
  List<Task> JoinTasks = new();
  public Task Join => Task.Run(() => Task.WhenAll(JoinTasks), Source.Token);
  T AddTask<T>(T task) where T : Task {
    JoinTasks.Add(task);
    return task;
  }

  // Child task spawning.
  public Task Run(TaskFunc f) => AddTask(Task.Run(() => f(this), Source.Token));
  public Task<T> Run<T>(TaskFunc<T> f) => AddTask(Task.Run(() => f(this), Source.Token));
  public Task RunChild(out TaskScope childScope, TaskFunc f) {
    Source.Token.ThrowIfCancellationRequested();
    childScope = new(this);
    var scope = childScope;
    return AddTask(Task.Run(() => f(scope), Source.Token));
  }

  // Basic control flow.
  public async Task Yield() {
    Source.Token.ThrowIfCancellationRequested();
    await Task.Yield();
  }
  public async Task Ticks(int ticks) {
    // Maybe this should be Repeat(ticks, ListenFor(FixedUpdateEvent)) ?
    // Cons: that might break if 2 events fire in a single Yield?
    // Pros: It guarantees to finish *after* N calls to FixedUpdate, whereas this version will terminate at any
    // point in the frame.
    int endTick = Timeval.TickCount + ticks;
    while (Timeval.TickCount < endTick)
      await Yield();
  }
  public Task Millis(int ms) => Task.Delay(ms, Source.Token);
  public Task Delay(Timeval t) => Ticks(t.Ticks);

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
    bool eventFired = false;
    void callback() { eventFired = true; };
    try {
      evt.Listen(callback);
      await Until(() => eventFired);
    } finally {
      evt.Unlisten(callback);
      Source.Token.ThrowIfCancellationRequested();
    }
  }
  public async Task<T> ListenFor<T>(IEventSource<T> evt) {
    Source.Token.ThrowIfCancellationRequested();
    bool eventFired = false;
    T eventResult = default;
    void callback(T v) { eventResult = v; eventFired = true; };
    try {
      evt.Listen(callback);
      await Until(() => eventFired);
      return eventResult;
    } finally {
      evt.Unlisten(callback);
      Source.Token.ThrowIfCancellationRequested();
    }
  }
  public async Task<int> ListenForAll<T>(IEventSource<T> evt, T[] results) {
    Source.Token.ThrowIfCancellationRequested();
    int eventFired = 0;
    int resultCount = 0;
    void callback(T v) {
      results[resultCount++] = v; eventFired = Timeval.TickCount;
    }
    try {
      evt.Listen(callback);
      // This is a hack to ensure we gather results for a full FixedUpdate tick.
      var startTick = Timeval.TickCount;
      await Until(() => eventFired > startTick);
      return resultCount;
    } finally {
      evt.Unlisten(callback);
      Source.Token.ThrowIfCancellationRequested();
    }
  }
}

public class TaskScopeTests : MonoBehaviour {
  public class TestData {
    public delegate Task<string> TestFunc(TaskScope scope);
    public string Name;
    public TestFunc Test;
    public string ExpectedOutput;

    public async Task Run() {
      using TaskScope scope = new();
      var output = await Test(scope);
      if (output != ExpectedOutput) {
        Debug.LogError($"Test {Name} FAILED:\n\tactual:\t{output}\n\texpect:\t{ExpectedOutput}");
      }
    }
  }

  async void Start() {
    Debug.Log($"Running {Tests.Length} tests...");
    foreach (var t in Tests) {
      await t.Run();
    }
    Debug.Log("Finished");
  }

  TestData[] Tests = {
    new() {
      Name = "basic",
      Test = async (TaskScope main) => {
        await main.Yield();
        return "done";
      },
      ExpectedOutput = "done"
    },

    new() {
      Name = "cancel",
      Test = async (TaskScope main) => {
        var output = "";
        await main.Run(
          async (main) => {
            output += "start;";
            try {
              main.Cancel();
              output += "beforeYield;";
              await main.Yield();
              output += "afterYield;";
            } catch (Exception) {
              output += "exception;";
            } finally {
              output += "finally;";
            }
          }
        );
        output += "end";
        return output;
      },
      ExpectedOutput = "start;beforeYield;exception;finally;end"
    },

    new() {
      Name = "any",
      Test = async (TaskScope main) => {
        var output = "";
        await main.Any(
          async (child) => {
            await child.Ticks(1);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Ticks(5);
              output += "child2;";
            } catch {
              output += "child2 cancel;";
            }
          }
        );
        await main.Yield();
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;child2 cancel;end"
    },

    new() {
      Name = "anyCancelled",
      Test = async (TaskScope main) => {
        var output = "";
        await main.Any(
          async (child) => {
            await child.Ticks(1);
            output += "child1;";
          },
          async (child) => {
            try {
              child.Cancel();
              output += "child2;";
              await child.Yield();
              output += "afterYield;";
            } catch {
              output += "child2 cancel;";
            }
          }
        );
        output += "end";
        return output;
      },
      ExpectedOutput = "child2;child2 cancel;end"
    },

    new() {
      Name = "anyCancelOuter",
      Test = async (TaskScope main) => {
        var output = "";
          try {
          await main.Any(
            async (child) => {
              await child.Ticks(1);
              output += "child1;";
              main.Cancel();
              await child.Yield();
              output += "child1 after;";
            }
          );
          output += "main after;";
        } catch {
          output += "main cancel;";
        }
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;main cancel;end"
    },

    new() {
      Name = "anyWhich",
      Test = async (TaskScope main) => {
        var output = "";
        var index = await main.Any(
          async (child) => {
            await child.Ticks(1);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Ticks(5);
              output += "child2;";
            } catch {
              output += "child2 cancel;";
            }
          }
        );
        await main.Yield();
        output += $"index={index};";
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;child2 cancel;index=0;end"
    },

    new() {
      Name = "all",
      Test = async (TaskScope main) => {
        var output = "";
        await main.All(
          async (child) => {
            await child.Ticks(1);
            output += "child1;";
          },
          async (child) => {
            await child.Ticks(2);
            output += "child2;";
          }
        );
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;child2;end"
    },

    new() {
      Name = "allCancelled",
      Test = async (TaskScope main) => {
        var output = "";
        await main.All(
          async (child) => {
            await child.Ticks(1);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Ticks(3);
              output += "child2;";
            } catch {
              output += "child2 cancel;";
            }
          },
          async (child) => {
            await child.Ticks(2);
            child.Cancel();
            output += "cancel;";
          }
        );
        await main.Yield();
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;cancel;child2 cancel;end"
    },

    new() {
      Name = "runchild",
      Test = async (TaskScope main) => {
        var output = "";
        var sub = main.RunChild(
          out var child,
          async (child) => {
            try {
              output += "sub;";
              await child.Ticks(1);
              output += "sub2;";
              await child.Ticks(5);
            } catch {
              output += "sub cancel;";
            }
          });
        var cancel = main.Run(async (main) => {
          await main.Ticks(2);
          child.Cancel();
          await main.Ticks(1);
          output += "cancel;";
        });
        await main.Join;
        output += "end";
        return output;
      },
      ExpectedOutput = "sub;sub2;sub cancel;cancel;end"
    },
    new() {
      Name = "join waits for runchild",
      Test = async (TaskScope main) => {
        var output = "";
        var sub = main.RunChild(
          out var child,
          async (child) => {
            try {
              output += "sub;";
              await child.Ticks(5);
              output += "sub2;";
            } catch {
              output += "sub cancel;";
            }
          });
        var cancel = main.Run(async (main) => {
          await main.Ticks(1);
          output += "main;";
        });
        await main.Join;
        output += "end";
        return output;
      },
      ExpectedOutput = "sub;main;sub2;end"
    },
    new() {
      Name = "returnval",
      Test = async (TaskScope main) => {
        var output = "";
        output += await main.Run(
          async (main) => {
            return await main.Run(
              async (main) => {
                await main.Ticks(1);
                return "cool;";
              });
          });
        output += "end";
        return output;
      },
      ExpectedOutput = "cool;end"
    },

    new() {
      Name = "listen",
      Test = async (TaskScope main) => {
        var output = "";
        EventSource evt = new();
        await main.All(
          async (child) => {
            await child.ListenFor(evt);
            output += "received;";
          },
          async (child) => {
            await child.Ticks(1);
            evt.Fire();
            output += "fired;";
          });
        output += "end";
        return output;
      },
      ExpectedOutput = "fired;received;end"
    },

    new() {
      Name = "listenValue",
      Test = async (TaskScope main) => {
        var output = "";
        EventSource<string> evt = new();
        await main.All(
          async (child) => {
            var result = await child.ListenFor(evt);
            output += $"received {result};";
          },
          async (child) => {
            await child.Ticks(1);
            evt.Fire("foo");
            output += "fired;";
          });
        output += "end";
        return output;
      },
      ExpectedOutput = "fired;received foo;end"
    },
    };
}