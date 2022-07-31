using System.Collections;

public class Choose : IEnumerator {
  bool Waiting;
  EventSource SourceA;
  EventSource SourceB;
  IEnumerator Routine;

  public Choose(EventSource a, EventSource b) {
    Waiting = true;
    SourceA = a;
    SourceB = b;
    SourceA.Action += ChooseLeft;
    SourceB.Action += ChooseRight;
    Routine = MkRoutine();
  }

  ~Choose() {
    SourceA.Action -= ChooseLeft;
    SourceB.Action -= ChooseRight;
    Routine = null;
    Waiting = false;
  }

  public bool Value { get; private set; }
  public object Current { get => Routine.Current; }
  public bool MoveNext() => Routine.MoveNext();
  public void Reset() => Routine.Reset();

  void ChooseLeft() {
    Value = true;
    Waiting = false;
    SourceA.Action -= ChooseLeft;
    SourceB.Action -= ChooseRight;
  }

  void ChooseRight() {
    Value = false;
    Waiting = false;
    SourceA.Action -= ChooseLeft;
    SourceB.Action -= ChooseRight;
  }

  IEnumerator MkRoutine() {
    while (Waiting) {
      yield return null;
    }
  }
}

public class Choose<A,B> : IEnumerator {
  bool Waiting;
  EventSource<A> SourceA;
  EventSource<B> SourceB;
  IEnumerator Routine;

  public Choose(EventSource<A> a, EventSource<B> b) {
    Waiting = true;
    SourceA = a;
    SourceB = b;
    SourceA.Action += ChooseA;
    SourceB.Action += ChooseB;
    Routine = MkRoutine();
  }

  ~Choose() {
    SourceA.Action -= ChooseA;
    SourceB.Action -= ChooseB;
    Routine = null;
    Waiting = false;
  }

  public bool Value { get; private set; }
  public A FirstResult { get; private set; }
  public B SecondResult { get; private set; }
  public object Current { get => Routine.Current; }
  public bool MoveNext() => Routine.MoveNext();
  public void Reset() => Routine.Reset();

  void ChooseA(A a) {
    Value = true;
    FirstResult = a;
    Waiting = false;
    SourceA.Action -= ChooseA;
    SourceB.Action -= ChooseB;
  }

  void ChooseB(B b) {
    Value = false;
    SecondResult = b;
    Waiting = false;
    SourceA.Action -= ChooseA;
    SourceB.Action -= ChooseB;
  }

  IEnumerator MkRoutine() {
    while (Waiting) {
      yield return null;
    }
  }
}

public class Switch : IEnumerator {
  EventSource Source0;
  EventSource Source1;
  EventSource Source2;
  EventSource Source3;
  EventSource Source4;
  EventSource Source5;
  IEnumerator Routine;

  public Switch(
  EventSource s0,
  EventSource s1,
  EventSource s2 = null,
  EventSource s3 = null,
  EventSource s4 = null,
  EventSource s5 = null) {
    Value = -1;
    Source0 = s0;
    Source1 = s1;
    Source2 = s2;
    Source3 = s3;
    Source4 = s4;
    Source5 = s5;
    Source0.Action += Choose0;
    Source1.Action += Choose1;
    if (Source2 != null) Source2.Action += Choose2;
    if (Source3 != null) Source3.Action += Choose3;
    if (Source4 != null) Source4.Action += Choose4;
    if (Source5 != null) Source5.Action += Choose5;
    Routine = MkRoutine();
  }

  ~Switch() {
    Source0.Action -= Choose0;
    Source1.Action -= Choose1;
    if (Source2 != null) Source2.Action -= Choose2;
    if (Source3 != null) Source3.Action -= Choose3;
    if (Source4 != null) Source4.Action -= Choose4;
    if (Source5 != null) Source5.Action -= Choose5;
    Routine = null;
  }

  public int Value { get; private set; }
  public object Current { get => Routine.Current; }
  public bool MoveNext() => Routine.MoveNext();
  public void Reset() => Routine.Reset();

  void YieldWith(int n) {
    Value = n;
    Source0.Action -= Choose0;
    Source1.Action -= Choose1;
    if (Source2 != null) Source2.Action -= Choose2;
    if (Source3 != null) Source3.Action -= Choose3;
    if (Source4 != null) Source4.Action -= Choose4;
    if (Source5 != null) Source5.Action -= Choose5;
  }
  void Choose0() => YieldWith(0);
  void Choose1() => YieldWith(1);
  void Choose2() => YieldWith(2);
  void Choose3() => YieldWith(3);
  void Choose4() => YieldWith(4);
  void Choose5() => YieldWith(5);

  IEnumerator MkRoutine() {
    while (Value < 0) {
      yield return null;
    }
  }
}