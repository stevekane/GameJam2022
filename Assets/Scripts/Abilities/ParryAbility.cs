using System.Collections;
using UnityEngine;

public class ParryAbility : Ability {
  public Timeval BlockDuration = Timeval.FromAnimFrames(15, 30);
  public MeleeAbility RiposteAbility;
  Animator Animator;
  Defender Defender;

  public static InlineEffect Invulnerable { get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "ParryInvulnerable");
  }

  public IEnumerator Execute() {
    if (AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude > 0f)
      yield break;
    using (AddStatusEffect(Invulnerable)) {
      Animator.SetBool("Blocking", true);
      var hitEvent = Fiber.ListenFor(Defender.HitEvent);
      yield return Fiber.Any(hitEvent, new CountdownTimer(BlockDuration), ListenFor(Release));
      Animator.SetBool("Blocking", false);
      if (hitEvent.IsCompleted) {
        AbilityManager.Bundle.StartRoutine(Riposte);
        yield break;
      }
    }
  }

  public IEnumerator Release() => null;

  public IEnumerator Riposte() {
    using (Status.Add(Invulnerable)) {
      AbilityManager.TryInvoke(RiposteAbility.AttackStart);
      yield return Fiber.While(() => RiposteAbility.IsRunning);
    }
  }

  public override void OnStop() {
    Animator.SetBool("Blocking", false);
  }

  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
    Defender = GetComponentInParent<Defender>();
  }
}