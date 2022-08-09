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
  Bundle Routines = new();
  public AbilityManager AbilityManager;
  public AbilityTag Tags;
  public AbilityTag Cancels;
  public AbilityTag Blocks;
  public bool IsRunning { get => Routines.IsRunning; }
  public bool IsFiberRunning(Fiber f) => Routines.IsFiberRunning(f);
  public virtual void Activate() => Routines.StartRoutine(new Fiber(MakeRoutine()));
  public virtual void Stop() => Routines.StopAll();
  public System.Action Activator(System.Func<IEnumerator> mkRoutine) => () => Routines.StartRoutine(new Fiber(mkRoutine()));
  protected abstract IEnumerator MakeRoutine();
  public void StartRoutine(Fiber routine) => Routines.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Routines.StopRoutine(routine);
  void FixedUpdate() => Routines.Run();
}

public abstract class ChargedAbility : Ability {
  public virtual void ReleaseCharge() { }
}