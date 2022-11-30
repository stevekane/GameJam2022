using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Fiber;

namespace Core {
  class Dodge : IEnumerator {
    IEnumerator Enumerator;
    public IEventSource Release;
    public IEventSource Action;
    public object Current => Enumerator.Current;
    public void Reset() => throw new NotSupportedException();
    public bool MoveNext() => Enumerator.MoveNext();
    public Dodge(IEventSource release, IEventSource action) {
      Release = release;
      Action = action;
      Enumerator = Routine();
    }
    public IEnumerator Routine() {
      bool held = true;
      IEnumerator Dash() {
        while (held) {
          var onRelease = Until(() => !held);
          var onDash = ListenFor(Action);
          var outcome = SelectTask(onRelease, onDash);
          yield return outcome;
          if (outcome.Value == onDash) {
            Debug.Log("Dash begun");
            yield return Wait(Timeval.FromSeconds(5f));
            Debug.Log("Dash complete");
          }
        }
      }
      IEnumerator HandleRelease() {
        yield return ListenFor(Release);
        Debug.Log("Release");
        held = false;
      }
      yield return All(Dash(), HandleRelease());
      Debug.Log("Ability ended");
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
    public InputManager InputManager;

    Fiber Routine;

    // async void Start() => await Root();
    void Start() {
      var release = InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown);
      var action = InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown);
      var dodge = new Dodge(release, action);
      Routine = new Fiber(dodge);
    }
    void FixedUpdate() => Routine.MoveNext();

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