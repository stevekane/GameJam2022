using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;

namespace Test {
  public interface ITask : IEnumerator { }

  public readonly struct FrameInfo {
    public readonly int Frame;
    public FrameInfo(int frame) => Frame = frame;
    public FrameInfo Step(ref FrameInfo f) => new FrameInfo(f.Frame+1);
  }

  [Serializable]
  public readonly struct TimeInfo {
    public readonly float DT;
    public readonly float Total;
    public TimeInfo(float dt, float total) => (DT, Total) = (dt, total);
    public TimeInfo Step(float dt, ref TimeInfo t) => new TimeInfo(dt, t.Total+dt);
  }

  // Core or platform tasks
  public class Clock : ITask {
    public FrameInfo FrameInfo = new FrameInfo(0);
    public TimeInfo TimeInfo = new TimeInfo(0, 0);
    public EventSource<FrameInfo> OnFixedUpdate = new();
    public EventSource<TimeInfo> OnUpdate = new();
    public bool MoveNext() => true;
    public void Reset() => throw new NotSupportedException("Reset clock not supported");
    public object Current { get => null; }
    public void FixedUpdate() {
      FrameInfo = FrameInfo.Step(ref FrameInfo);
      OnFixedUpdate.Fire(FrameInfo);
    }
    public void Update(float dt) {
      TimeInfo = TimeInfo.Step(dt, ref TimeInfo);
      OnUpdate.Fire(TimeInfo);
    }
  }

  public class NullTask : ITask {
    public bool MoveNext() => false;
    public void Reset() { }
    public object Current { get => null; }
  }

  public class ParallelTask : ITask {
    ITask P;
    ITask Q;
    public ParallelTask(ITask p, ITask q) => (P, Q) = (p, q);
    public bool MoveNext() => P.MoveNext() || Q.MoveNext();
    public void Reset() => throw new NotSupportedException("Reset Parallel not supported");
    public object Current { get => null; }
  }

  /*
  Let's get away from details and distractions of the host language, C#, and ask what
  we are actually trying to achieve. The objective, broadly speaking, is to define logic
  as flock of communicating processes. As a raw primitive, the process which just exists
  and may or may not listen on and send information along names is not much restrained
  or tamed for coordination into a larger collective. Much like employee handbooks, legal
  codes, and lining up to go to recess, we must impose some additional expectations and
  relationships on our processes to start to develop a set of useful building blocks
  from which we might assemble a large application.

  The simplest process is fully autonomous but also wholy inert: The null proces (written 0).
  This process is the logically-simplest process possible but also has no interesting properties
  except the critically important observation that P|0 = P meaning that the null process is
  effectively eliminated from consideration.

  The next fundamental process is the act of placing processes in sequence P.Q. This means
  that a process must have a notion of being completed which raises an important question:
  "What exactly is the foundational element of the idea of completeness?". The answer is
  communication. When a process recieves communication on a channel it is listening to then
  that listening event is said to have occurred and the processes defined to follow it sequentially
  then begin. This is written channel(message).P(message) where P(message) is a process
  that may depend on the recieved message.

  This is to say, the only thing that "takes time" is awaiting the arrival of a message from
  another process in parallel with the one that is waiting.

  channel.send<data> | channel.recv(x).P(x)
    ↓
  P(data)

  If we wish to allow unbounded numbers of subscribers then we might want to use another
  kind of process constructor called replicate (written !P).

  !channel.send<data> | channel.recv(x).P(x) | channel.recv(y).Q(y)
    ↓
  !channel.send<data> | P(data) | Q(data)

  We may also put processes in a superposition such that we cannot say which of the two
  will occur only that one of them will. P+Q

  channel.recv(x).P(x)+Q(x)

  We are saying here that after this process receives data on the channel it will either
  become P(x) or Q(x). The strange thing about this though is that we have no way here of saying
  which it will actually be... This feels like a sort of "super powerful" flavor of non-determinism
  that we have introduced to our language but that seems to have little to do with ordinary
  programming tasks.

  Instead, we might include a choice operator which has a computational meaning (conditional
  branching) It would look like this [x==y]P and [x/=y]Q and maybe even have a shorthand for a common
  if-then-else structure: if x==y then P else Q or perhaps even allow pattern-matching like
  case x of
    y => P
    z => Q
    otherwise 0
  */

  public class Listen<T> : ITask {
    bool IsRunning = true;
    EventSource<T> Source;
    Action Action;
    void OnCall(T t) {
      IsRunning = false;
      Value = t;
      Source.Unlisten(OnCall);
    }
    public Listen(EventSource<T> source) {
      Source = source;
      Source.Listen(OnCall);
    }
    public bool MoveNext() => IsRunning;
    public object Current { get => Value; }
    public void Reset() => throw new NotSupportedException("Cannot reset Listen");
    public T Value { get; set; }
  }

  public class Tasks : MonoBehaviour {
    // TODO: Cannot be stopped...
    static async Task<T> ListenFor<T>(EventSource<T> es) {
      var running = true;
      T value = default;
      void OnCall(T t) {
        running = false;
        es.Unlisten(OnCall);
        value = t;
      }
      es.Listen(OnCall);
      while (running) {
        await Task.Yield();
      }
      return value;
    }

    // TODO: Cannot be stopped...
    static async Task<(bool, A, B)> Listen<A, B>(EventSource<A> esa, EventSource<B> esb) {
      var running = true;
      (bool, A, B) value = (true, default, default);
      void OnCallA(A a) {
        running = false;
        esa.Unlisten(OnCallA);
        esb.Unlisten(OnCallB);
        value.Item1 = true;
        value.Item2 = a;
      }
      void OnCallB(B b) {
        running = false;
        esa.Unlisten(OnCallA);
        esb.Unlisten(OnCallB);
        value.Item1 = false;
        value.Item3 = b;
      }

      esa.Listen(OnCallA);
      esb.Listen(OnCallB);
      while (running) {
        await Task.Yield();
      }
      return value;
    }

    static async Task Cancellation(CancellationToken token) {
      while (!token.IsCancellationRequested) {
        await Task.Yield();
      }
    }

    static async Task UntilCancelled(CancellationToken token, Func<Task> t) {
      while (!token.IsCancellationRequested) {
        await t();
      }
    }

    static async Task OnClock(EventSource<CancellationToken> cancel, EventSource<FrameInfo> clock, Transform t) {
      var (cancelled, token, dt) = await Listen(cancel, clock);
      if (cancelled) {
        Debug.Log("Cancelled");
      } else {
        t.RotateAround(t.position, Vector3.up, 1);
        await OnClock(cancel, clock, t);
      }
    }

    public Clock Clock = new Clock();
    public Transform Cube;
    public CancellationTokenSource Source = new CancellationTokenSource();
    public EventSource<CancellationToken> Cancel = new EventSource<CancellationToken>();
    void Update() => Clock.Update(Time.deltaTime);
    void FixedUpdate() => Clock.FixedUpdate();
    async void Start() => await OnClock(Cancel, Clock.OnFixedUpdate, Cube);

    // Enables you to cancel this infinite task from the Editor
#if UNITY_EDITOR
    [ContextMenu("Cancel")]
    void DoCancel() => Cancel.Fire(Source.Token);
#endif

    async void EveryFrame(CancellationToken token) {
      await UntilCancelled(token, async delegate {
        var frameInfo = await ListenFor(Clock.OnFixedUpdate);
        Cube.RotateAround(Cube.position, Vector3.up, 1);
      });
    }
  }
}