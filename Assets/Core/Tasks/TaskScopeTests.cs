using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TaskScopeTests : MonoBehaviour {
  public class TestData {
    public delegate Task<string> TestFunc(TaskScope scope);
    public string Name;
    public TestFunc Test;
    public string ExpectedOutput;

    public async Task Run(TaskScope scope) {
      //Debug.Log($"Test {Name} running...");
      var child = new TaskScope(scope);
      var output = await Test(child);
      if (output != ExpectedOutput) {
        Debug.LogError($"Test {Name} FAILED:\n\tactual:\t{output}\n\texpect:\t{ExpectedOutput}");
      }
    }
  }

  TaskScope Scope = new();
  void Start() {
    Scope.Start(StartTest);
  }

  async Task StartTest(TaskScope scope) {
    Debug.Log($"Running {Tests.Length} tests...");
    foreach (var t in Tests) {
      Debug.Log($"Test {t.Name}...");
      await t.Run(scope);
    }
    Debug.Log("Finished");
  }

  TestData[] Tests = {
    new() {
      Name = "basic",
      Test = async (TaskScope main) => {
        await main.Tick();
        return "done";
      },
      ExpectedOutput = "done"
    },

    new() {
      Name = "multitick",
      Test = async (TaskScope main) => {
        var output = "";
        Timeval.TickCount = 0;
        await main.All(
          async (child) => {
            try {
              output += $"child1 {Timeval.TickCount};";
              await child.Tick();
            } finally {
              output += $"child1 finally {Timeval.TickCount} {TaskScheduler.Current == TaskManager.Scheduler};";
            }
          },
          async (child) => {
            try {
              output += $"child2 {Timeval.TickCount};";
              await child.Tick();
            } finally {
              output += $"child2 finally {Timeval.TickCount} {TaskScheduler.Current == TaskManager.Scheduler};";
            }
          },
          async (child) => {
            try {
              output += $"child3 {Timeval.TickCount};";
              await child.Tick();
              output += $"child3 {Timeval.TickCount};";
              await child.Tick();
            } finally {
              output += $"child3 finally {Timeval.TickCount} {TaskScheduler.Current == TaskManager.Scheduler};";
            }
          }
        );
        await main.Tick();
        output += $"end {Timeval.TickCount}";
        return output;
      },
      ExpectedOutput = "child1 0;child2 0;child3 0;child1 finally 1 True;child2 finally 1 True;child3 1;child3 finally 2 True;end 3"
    },

    new() {
      Name = "cancel",
      Test = async (TaskScope main) => {
        var output = "";
        await main.Run(
          async (main) => {
            output += "start;";
            try {
              await main.Millis(16*1);
              main.Cancel();
              output += "before Tick;";
              await main.Tick();
              output += "after Tick;";
            } catch (OperationCanceledException) {
              output += "exception;";
            } finally {
              output += "finally;";
            }
          }
        );
        output += "end";
        return output;
      },
      ExpectedOutput = "start;before Tick;exception;finally;end"
    },

    new() {
      Name = "cancelOther",
      Test = async (TaskScope main) => {
        var output = "";
        Timeval.TickCount = 0;
        await main.Any(
          async (child) => {
            try {
              output += $"child1 {Timeval.TickCount};";
              await child.Ticks(5);
              output += "child1 notreached;";
            } finally {
              output += $"child1 finally {Timeval.TickCount};";
            }
          },
          async (child) => {
            try {
              output += $"child2 start {Timeval.TickCount};";
              await child.Ticks(1);
              output += $"child2 cancelling {Timeval.TickCount};";
              child.Cancel();
              output += $"child2 awaiting {Timeval.TickCount};";
              await child.Tick();
              output += $"child2 notreached;";
            } finally {
              output += $"child2 finally {Timeval.TickCount};";
            }
          }
        );
        await main.Tick();
        output += $"end {Timeval.TickCount}";
        return output;
      },
      ExpectedOutput = "child1 0;child2 start 0;child2 cancelling 1;child2 awaiting 1;child2 finally 1;child1 finally 2;end 2"
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
        await main.Tick();
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
            await child.Millis(16*1);
            output += "child1;";
          },
          async (child) => {
            try {
              child.Cancel();
              output += "child2;";
              await child.Tick();
              output += "afterTick;";
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
              await child.Millis(16*1);
              output += "child1;";
              main.Cancel();
              await child.Tick();
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
            await child.Millis(16*1);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Millis(16*5);
              output += "child2;";
            } catch {
              output += "child2 cancel;";
            }
          }
        );
        await main.Tick();
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
            await child.Millis(16*1);
            output += "child1;";
          },
          async (child) => {
            await child.Millis(16*2);
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
            await child.Millis(16*1);
            output += "child1;";
          },
          async (child) => {
            try {
              await child.Millis(16*4);
              output += "child2;";
            } catch {
              output += "child2 cancel;";
            }
          },
          async (child) => {
            await child.Millis(16*2);
            child.Cancel();
            output += "cancel;";
          }
        );
        await main.Tick();
        output += "end";
        return output;
      },
      ExpectedOutput = "child1;child2 cancel;cancel;end"
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
              await child.Millis(16*1);
              output += "sub2;";
              await child.Millis(16*5);
            } catch {
              output += "sub cancel;";
            }
          });
        var cancel = main.Run(async (main) => {
          await main.Millis(16*2);
          child.Cancel();
          await main.Millis(16*1);
          output += "cancel;";
        });
        await main.AllTask(sub, cancel);
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
              await child.Millis(16*5);
              output += "sub2;";
            } catch {
              output += "sub cancel;";
            }
          });
        var cancel = main.Run(async (main) => {
          await main.Millis(16*1);
          output += "main;";
        });
        await main.AllTask(sub, cancel);
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
                await main.Millis(16*1);
                return "cool;";
              });
          });
        output += "end";
        return output;
      },
      ExpectedOutput = "cool;end"
    },
    new() {
      Name = "exceptionsInAll",
      Test = async (TaskScope main) => {
        var caught = false;
        var output = "";
        async Task Throw1(TaskScope scope) {
          await scope.Tick();
          throw new Exception("Throw1");
        }
        async Task Throw2(TaskScope scope) {
          await scope.Millis(3000);
          output += "didnt throw;";
          throw new Exception("Throw2");
        }
        async Task Behavior(TaskScope scope) {
          try {
            await scope.All(
              Waiter.Repeat(Throw1),
              Waiter.Repeat(Throw2));
          } catch (Exception e) {
            output += $"caught {e.Message};";
            caught = true;
          }
        }
        main.Start(Behavior);
        while (!caught) {
          await main.Tick();
        }
        output += "done";
        return output;
      },
      ExpectedOutput = "caught Throw1;done"
    },
    new() {
      Name = "exceptionsInAny",
      Test = async (TaskScope main) => {
        var done = false;
        var output = "";
        async Task Throw1(TaskScope scope) {
          await scope.Tick();
          throw new Exception("Throw1");
        }
        async Task Throw2(TaskScope scope) {
          await scope.Millis(3000);
          output += "didnt throw;";
          throw new Exception("Throw2");
        }
        async Task Behavior(TaskScope scope) {
          try {
            await scope.Any(
              Waiter.Repeat(Throw1),
              Waiter.Repeat(Throw2));
          } catch (Exception e) {
            output += $"caught {e.Message};";
          } finally {
            done = true;
          }
        }
        main.Start(Behavior);
        while (!done) {
          await main.Tick();
        }
        output += "done";
        return output;
      },
      ExpectedOutput = "caught One or more errors occurred. (Throw1);done"
    },
  };
}