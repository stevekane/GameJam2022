using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TaskScope : IDisposable {
  List<Task> Tasks = new();
  CancellationTokenSource Source = new();

  public Task Join => Task.Run(() => Task.WhenAll(Tasks), Source.Token);
  public void Cancel() => Source.Cancel();
  public void Dispose() {
    Source.Cancel();
    Source.Dispose();
  }
  public Task Run(Func<TaskScope, Task> f) {
    var task = Task.Run(() => f(this), Source.Token);
    Tasks.Add(task);
    return task;
  }
  public Task SubRun(Func<TaskScope, Task> f, out TaskScope subScope) {
    Source.Token.ThrowIfCancellationRequested();
    subScope = new();
    var scope = subScope;
    var task = Task.Run(async () => { await f(scope); scope.Dispose(); }, Source.Token);
    Tasks.Add(task);
    return task;
  }
  public Task Delay(int ms) => Task.Delay(ms, Source.Token);
  public async Task Yield() {
    Source.Token.ThrowIfCancellationRequested();
    await Task.Yield();
  }
  public async Task Any(params Func<TaskScope, Task>[] fs) {
    Source.Token.ThrowIfCancellationRequested();
    using TaskScope scope = new();
    try {
      await Task.WhenAny(fs.Select(f => f(scope)));
    } finally {
    }
  }
  public async Task All(params Func<TaskScope, Task>[] fs) {
    Source.Token.ThrowIfCancellationRequested();
    using TaskScope scope = new();
    try {
      await Task.WhenAll(fs.Select(f => f(scope)));
    } finally {
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
        Debug.LogError($"Test {Name} FAILED:\n\tgot: {output}\n\texpected: {ExpectedOutput}");
      }
    }
  }

  async void Start() {
    Debug.Log($"Running {Tests.Length} tests...");
    foreach (var t in Tests)
      await t.Run();
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
            await child.Delay(50);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Delay(100);
              output += "child2;";
            } catch {
              output += "child2 cancel;";
            }
          }
        );
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
            await child.Delay(50);
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
      Name = "all",
      Test = async (TaskScope main) => {
        var output = "";
        await main.All(
          async (child) => {
            await child.Delay(50);
            output += "child1;";
          },
          async (child) => {
            await child.Delay(100);
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
            await child.Delay(50);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Delay(200);
              output += "child2;";
            } catch {
              output += "child2 catch;";
            }
          },
          async (child) => {
            await child.Delay(100);
            child.Cancel();
            output += "cancel;";
          }
        );
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;child2 catch;cancel;end"
    },
    };
}
