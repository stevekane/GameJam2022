using System.Collections;
using UnityEngine;

public interface IAbility {
  public abstract void Play();
  public abstract void Pause();
  public abstract void Stop();
  public abstract void Tick();
}

public interface BaseAbility : IAbility, IEnumerator {}

public abstract class Ability : MonoBehaviour {
  Fiber? Routine;
  public AbilityManager AbilityManager;
  public AbilityTag Tags;
  public AbilityTag Cancels;
  public AbilityTag Blocks;
  public bool IsRunning { get => Routine.HasValue; }
  public virtual void Activate() => Routine = new Fiber(MakeRoutine());
  public virtual void Stop() => Routine = null;
  protected abstract IEnumerator MakeRoutine();
  void FixedUpdate() => Routine?.MoveNext();
}

public abstract class ChargedAbility : Ability {
  public virtual void ReleaseCharge() { }
}