using System.Collections;
using UnityEngine;

public abstract class Ability : MonoBehaviour {
  Fiber? Routine;
  public bool IsRunning { get => Routine.HasValue; }
  public virtual void Activate() => Routine = new Fiber(MakeRoutine());
  public virtual void Stop() => Routine = null;
  protected abstract IEnumerator MakeRoutine();
  void FixedUpdate() => Routine?.MoveNext();
}

public abstract class ChargedAbility : Ability {
  public virtual void ReleaseCharge() { }
}