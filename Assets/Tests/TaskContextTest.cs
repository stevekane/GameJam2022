using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core {
  public class TaskContext {
    CancellationTokenSource Source = new();
    Action OnCancel;
    public async Task Run(Func<TaskContext, Task> action) {
      await Task.Run(() => action(this), Source.Token);
      IfCancelledThrow();
    }
    public async Task Delay(int ms) {
      await Task.Delay(ms, Source.Token);
      IfCancelledThrow();
    }
    public async Task Any(params Func<TaskContext, Task>[] xs) {
      TaskContext context = new();
      try {
        // Ah we need to run every descendent task in this new context. This is no good.
        await Task.WhenAny(xs.Select(x => context.Run(x)));
      } catch (TaskCanceledException) {
        Debug.Log("Any cancelled");
      } finally {
        Debug.Log("Any finally");
        context.Cancel();
      }
      IfCancelledThrow();
    }
    public async Task Yield() {
      await Task.Yield();
      IfCancelledThrow();
    }

    public void Cancel() {
      DidCancel();  // must come first
      Source.Cancel();
    }
    public void DidCancel() {
      OnCancel?.Invoke();
      OnCancel = null;
    }
    public void IfCancelledThrow() {
      if (Source.Token.IsCancellationRequested)
        throw new TaskCanceledException();
    }

    // Exception-handling seems like a better pattern for cancellation/cleanup
    //public IDisposable WithCleanup(Action cleanup) {
    //  return new TaskCleanupContext(this, cleanup);
    //}
    //class TaskCleanupContext : IDisposable {
    //  TaskContext Context;
    //  Action OnCancel;
    //  public void Dispose() => Context.UnregisterOnCancel(OnCancel);
    //  public TaskCleanupContext(TaskContext context, Action onCancel) {
    //    (Context, OnCancel) = (context, onCancel);
    //    Context.RegisterOnCancel(OnCancel);
    //  }
    //}
    //void RegisterOnCancel(Action oncancel) => OnCancel += oncancel;
    //void UnregisterOnCancel(Action oncancel) => OnCancel -= oncancel;
  }

  public class TaskContextTest : MonoBehaviour {
    public int CancelAfterMs = 10000;
    public bool CancelInGrandchild;
    public InputManager InputManager;
    TaskContext Jobs = new();

    async void Start() => await Root2();

    async Task Root2() {
      Debug.Log("Root Start");
      try {
        await Jobs.Any(Child1, Child2, (ctx) => CancelAfter(ctx, CancelAfterMs));
      } catch (TaskCanceledException) {
        Debug.Log("Root cancel");
      }
      Debug.Log("Root Stop");
    }

    async Task Child1(TaskContext ctx) {
      Debug.Log("Child1 start");
      try {
        await ctx.Delay(1000);
        Debug.Log("Child1 done");
      } catch (TaskCanceledException) {
        Debug.Log($"Child1 cancel");
      } finally {
        Debug.Log("Child1 cleanup");
      }
    }

    async Task Child2(TaskContext ctx) {
      Debug.Log("Child2 start");

      try {
        await GrandChild2(ctx);
        await ctx.Delay(5000);
        Debug.Log("Child2 done");
      } catch (TaskCanceledException) {
        Debug.Log("Child2 cancel");
      } finally {
        Debug.Log("Child2 cleanup");
      }
    }

    async Task CancelAfter(TaskContext ctx, int ms) {
      await ctx.Delay(ms);
      Debug.Log("Cancelling");
      ctx.Cancel();
    }

    async Task GrandChild2(TaskContext ctx) {
      Debug.Log("GrandChild2 start");
      try {
        if (CancelInGrandchild)
          ctx.Cancel();
        await ctx.Yield();
        Debug.Log("GrandChild2 done");
      } catch (TaskCanceledException) {
        Debug.Log("GrandChild2 cancelled");
      } finally {
        Debug.Log("GrandChild2 cleaned up");
      }
    }
  }
}