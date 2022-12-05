using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core {
  public static class CancellationTokenExtensions {
    public static void IfCancelledThrow(this CancellationToken token, string message) {
      if (token.IsCancellationRequested) {
        throw new Exception(message);
      }
    }
  }

  public class JobContext : IDisposable {
    CancellationTokenSource Source;
    public JobContext() => Source = new();
    public JobContext(JobContext parent) => Source = parent.Source;
    public void Cancel() => Source.Cancel();
    public Task Run(Func<Task> f) => Task.Run(f, Source.Token);
    public Task Delay(int ms) => Task.Delay(ms, Source.Token);
    public async Task Sentinel() {
      while (!Source.Token.IsCancellationRequested) {
        await Task.Yield();
      }
    }
    public async Task Any(Func<JobContext, Task> fa, Func<JobContext,Task> fb) {
      using var context = new JobContext();
      try {
        await Task.WhenAny(new Task[3] {
          Sentinel(),
          context.Run(() => fa(context)),
          context.Run(() => fb(context)),
        });
      } finally {
        context.Cancel();
      }
    }
    public void Dispose() => Source.Dispose();
  }

  public class Core : MonoBehaviour {
    [SerializeField] InputManager InputManager;

    async Task G(JobContext ctx, int ms) {
      await ctx.Delay(ms);
    }

    async Task F(JobContext ctx, bool flipflop, int ms) {
      while (true) {
        await ctx.Run(() => G(ctx, ms));
        Debug.Log(flipflop);
        flipflop = !flipflop;
      }
    }

    JobContext MainContext = new();

    async Task Wait(JobContext ctx, int ms) {
      try {
        await ctx.Delay(ms);
      } catch (Exception e) {
        Debug.LogWarning($"Wait{ms} Error {e.Message}");
      } finally {
        Debug.Log($"Wait{ms} finally");
      }
    }

    async void Start() {
      try {
        InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(MainContext.Cancel);
        await MainContext.Any((JobContext ctx) => Wait(ctx, 5000), (JobContext ctx) => Wait(ctx, 10000));
      } catch (Exception e) {
        Debug.Log($"Start {e.Message}");
      }
    }

    void OnDestroy() {
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(MainContext.Cancel);
      MainContext.Cancel();
      MainContext.Dispose();
    }
  }
}