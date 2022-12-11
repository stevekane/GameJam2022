using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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
              await main.Ticks(1);
              main.Cancel();
              output += "before yield;";
              await main.Yield();
              output += "after yield;";
            } catch {
              output += "exception;";
            } finally {
              output += "finally;";
            }
          }
        );
        output += "end";
        return output;
      },
      ExpectedOutput = "start;before yield;exception;finally;end"
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
              await child.Ticks(4);
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
      ExpectedOutput = "received;fired;end"
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
      ExpectedOutput = "received foo;fired;end"
    },
    new() {
      Name = "listenAll",
      Test = async (TaskScope main) => {
        var output = "";
        EventSource<string> evt = new();
        await main.All(
          async (child) => {
            var results = new string[16];
            var count = await child.ListenForAll(evt, results);
            output += $"received {string.Join(",", results.Take(count))};";
          },
          async (child) => {
            evt.Fire("1");
            evt.Fire("2");
            evt.Fire("3");
            await child.Tick();
            evt.Fire("4");
            output += "fired;";
          });
        output += "end";
        return output;
      },
      ExpectedOutput = "received 1,2,3;fired;end"
    },
  };
}