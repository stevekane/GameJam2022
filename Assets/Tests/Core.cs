using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Fiber;

namespace Core {
  class Dodge : FiberAbility {
    public EventSource Release;
    public EventSource StickAction;
    public override void OnStop() {}
    public override float Score() => 1;
    public override IEnumerator Routine() {
      Bundle bundle = new Bundle();
      IEnumerator dash = null;
      void HandleDashRequest() {
        if (dash == null) {
          dash = Dash();
          bundle.StartRoutine(dash);
        }
      }
      IEnumerator Dash() {
        yield return Wait(Timeval.FromSeconds(0.5f));
        dash = null;
      }
      IEnumerator HandleStickAction() {
        yield return ListenFor(StickAction);
        HandleDashRequest();
      }
      bundle.StartRoutine(Any(Repeat(HandleStickAction), ListenFor(Release)));
      yield return bundle;
    }
  }

  class Cleanup: IDisposable {
    Action OnDispose;
    public void Dispose() => OnDispose.Invoke();
    public Cleanup(Action onDispose) => OnDispose = onDispose;
    public static Cleanup With(Action onDispose) => new Cleanup(onDispose);
  }

  public static class CancellationTokenExtensions {
    public static void IfCancelledThrow(this CancellationToken token, string message) {
      if (token.IsCancellationRequested) {
        throw new Exception(message);
      }
    }
  }

  public class Core : MonoBehaviour {
    public bool CancelImmediately;

    async void Start() => await Root();

    async Task Sentinel(CancellationToken a, CancellationTokenSource b) {
      while (!a.IsCancellationRequested) {
        await Task.Yield();
      }
      b.Cancel();
    }

    async Task Either(
    Func<CancellationToken,Task> fa,
    Func<CancellationToken,Task> fb,
    CancellationToken token) {
      using var source = new CancellationTokenSource();
      try {
        var tasks = new Task[3];
        tasks[0] = fa(source.Token);
        tasks[1] = fb(source.Token);
        tasks[2] = Sentinel(token, source);
        await Task.WhenAny(tasks);
      } finally {
        source.Cancel();
      }
    }

    async Task Root() {
      Debug.Log("Root Start");
      using var source = new CancellationTokenSource();
      try {
        if (CancelImmediately) {
          source.Cancel();
        }
        await Either(Child1, Child2, source.Token);
      } finally {
        Debug.Log("Root Stop");
      }
    }

    async Task Child1(CancellationToken token) {
      await Task.Delay(2000, token);
      Debug.Log("End of child1");
    }

    async Task Child2(CancellationToken token) {
      await GrandChild2(token);
      await Task.Delay(1000, token);
      Debug.Log("End of child2");
    }

    async Task GrandChild2(CancellationToken token) {
      try {
        token.IfCancelledThrow("GrandChild2 aborted");
        await Task.Yield();
      } finally {
        Debug.Log("GrandChild2 cleaned up");
      }
    }
  }
}