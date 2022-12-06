using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Core {
  public class TaskScope : IDisposable {
    public List<Task> Tasks = new();
    public Task Join => Task.Run(() => Task.WhenAll(Tasks), Source.Token);
    public readonly CancellationTokenSource Source = new();
    public void Cancel() => Source.Cancel();
    public void Dispose() => Source.Dispose();
    public Task Async<T>(Func<TaskScope, Task<T>> f) {
      var task = Task.Run(() => f(this), Source.Token);
      Tasks.Add(task);
      return task;
    }
    public Task Run(Func<TaskScope, Task> f) {
      var task = Task.Run(() => f(this), Source.Token);
      Tasks.Add(task);
      return task;
    }
    public Task SubRun(Func<TaskScope, Task> f, out TaskScope innerctx) {
      var ctx = new TaskScope();
      innerctx = ctx;
      var task = Task.Run(async delegate { await f(ctx); ctx.Dispose(); }, Source.Token);
      Tasks.Add(task);
      return task;
    }
    public Task Delay(int ms) => Task.Delay(ms, Source.Token);
    public async Task Any(params Func<TaskScope, Task>[] fs) {
      using var ctx = new TaskScope();
      try {
        Source.Token.ThrowIfCancellationRequested();
        await Task.WhenAny(fs.Select(f => f(ctx)));
      } finally {
        ctx.Cancel();
      }
    }
    public async Task All(params Func<TaskScope, Task>[] fs) {
      using var ctx = new TaskScope();
      try {
        Source.Token.ThrowIfCancellationRequested();
        await Task.WhenAll(fs.Select(f => f(ctx)));
      } finally {
        ctx.Cancel();
      }
    }
    public async Task All(params Task[] tasks) {
      using var ctx = new TaskScope();
      try {
        await Task.WhenAll(tasks);
      } finally {
        ctx.Cancel();
      }
    }
  }

  public class Core : MonoBehaviour {
    TaskScope MainContext = new();

    public static async Task Dash(TaskScope ctx) {
      await ctx.Run(async delegate {
        try {
          Debug.Log("DashStart");
          await ctx.Delay(1000);
          Debug.Log("DashEnd");
          await ctx.Delay(500);
          Debug.Log("DashRecovery");
        } catch (Exception) {
          Debug.Log("Dash Cancel");
        } finally {
          Debug.Log("Disable dash effects");
        }
      });
    }

    void Start() {
      var job = MainContext.SubRun(Dash, out var dashCtx);
      var can = MainContext.Run(async delegate {
        await MainContext.Delay(1000);
        dashCtx.Cancel();
      });
    }

    void OnDestroy() {
      if (!MainContext.Source.IsCancellationRequested) {
        MainContext.Source.Cancel();
      }
      MainContext.Dispose();
    }
  }
}