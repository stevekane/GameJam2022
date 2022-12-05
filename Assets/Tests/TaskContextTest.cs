using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core {
  public class TaskCancelled : Exception { }
  public class TaskContext {
    CancellationTokenSource Source = new();
    Action OnCancel;
    public async Task Run(Func<Task> action) {
      await Task.Run(action, Source.Token);
      IfCancelledThrow();
    }
    public async Task Delay(int ms) {
      await Task.Delay(ms, Source.Token);
      IfCancelledThrow();
    }
    public async Task Any(params Func<Task>[] xs) {
      await Task.WhenAny(xs.Select(x => Run(x)));
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
        throw new TaskCancelled();
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
    public bool CancelImmediately;
    public InputManager InputManager;
    TaskContext Jobs = new();

    async void Start() => await Root2();

    async Task Root2() {
      Debug.Log("Root Start");
      try {
        await Jobs.Any(Child1, Child2, () => CancelAfter(1050));
      } catch (TaskCancelled) {
        Debug.Log("Root cancel");
      }
      Debug.Log("Root Stop");
    }

    async Task Child1() {
      Debug.Log("Child1 start");
      try {
        await Jobs.Delay(2000);
        Debug.Log("Child1 done");
      } catch (TaskCancelled) {
        Debug.Log("Child1 cancel");
      }
    }

    async Task Child2() {
      Debug.Log("Child2 start");

      try {
        //Jobs.Cancel();
        await GrandChild2();
        await Jobs.Delay(1000);
        //Jobs.Cancel();
        await Jobs.Delay(1000);
        Debug.Log("Child2 done");
      } catch (TaskCancelled) {
        Debug.Log("Child2 cancel");
      }
    }

    async Task CancelAfter(int ms) {
      await Jobs.Delay(ms);
      Debug.Log("Cancelling");
      Jobs.Cancel();
    }

    async Task GrandChild2() {
      Debug.Log("GrandChild2 start");
      try {
        Jobs.Cancel();
        await Jobs.Yield();
        Debug.Log("GrandChild2 done");
      } catch (TaskCancelled) {
        Debug.Log("GrandChild2 cancelled");
      } finally {
        Debug.Log("GrandChild2 cleaned up");
      }
    }
  }
}