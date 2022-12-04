using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core {
  public class TaskContext {
    CancellationTokenSource Source = new();
    Action OnCancel;
    public Task Run(Action action) => Task.Run(action, Source.Token);
    public Task Delay(int ms) => Task.Delay(ms, Source.Token);
    public Task Any(Action a, Action b) => Task.WhenAny(new Task[] { Run(a), Run(b) });

    public void Cancel() {
      DidCancel();
      Source.Cancel();
    }
    public void DidCancel() {
      OnCancel?.Invoke();
      OnCancel = null;
    }
    public IDisposable WithCleanup(Action cleanup) {
      return new TaskCleanupContext(this, cleanup);
    }
    public void IfCancelledThrow() {
      if (Source.Token.IsCancellationRequested)
        throw new Exception();
    }

    class TaskCleanupContext : IDisposable {
      TaskContext Context;
      Action OnCancel;
      public void Dispose() => Context.UnregisterOnCancel(OnCancel);
      public TaskCleanupContext(TaskContext context, Action onCancel) {
        (Context, OnCancel) = (context, onCancel);
        Context.RegisterOnCancel(OnCancel);
      }
    }
    void RegisterOnCancel(Action oncancel) => OnCancel += oncancel;
    void UnregisterOnCancel(Action oncancel) => OnCancel -= oncancel;
  }

  public class TaskContextTest : MonoBehaviour {
    public bool CancelImmediately;
    public InputManager InputManager;
    TaskContext Jobs = new();

    async void Start() => await Root2();

    async Task Root2() {
      Debug.Log("Root Start");
      using var cleanup = Jobs.WithCleanup(() => Debug.Log("Root cancel"));
      await Jobs.Any(Child1, Child2);
      Debug.Log("Root Stop");
    }

    async void Child1() {
      Debug.Log("Child1 start");
      using var cleanup = Jobs.WithCleanup(() => Debug.Log("Child1 cancel"));
      await Jobs.Delay(2000);
      Debug.Log("Child1 stop");
    }

    async void Child2() {
      Debug.Log("Child2 start");
      using var cleanupOuter = Jobs.WithCleanup(() => Debug.Log("Child2 cancel outer"));
      {
        using var cleanup = Jobs.WithCleanup(() => Debug.Log("Child2 cancel inner"));
        //await GrandChild2();
        await Jobs.Delay(1000);
      }
      Jobs.Cancel();
      await Jobs.Delay(1000);
      Debug.Log("Child2 stop");
    }

    async Task GrandChild2() {
      try {
        Jobs.IfCancelledThrow();
        await Task.Yield();
      } finally {
        Debug.Log("GrandChild2 cleaned up");
      }
    }
  }
}