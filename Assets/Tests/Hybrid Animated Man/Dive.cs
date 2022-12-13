using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Dive : Ability {
  [SerializeField] float FallSpeed;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] AnimationJobConfig WindupAnimation;
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent TriggerEvent;
  [SerializeField] Collider HitBox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] AnimationJobTask Animation = null;

  public static InlineEffect ScriptedMove => new(s => {
    s.HasGravity = false;
    s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
    s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
  }, "DiveMove");

  public async Task Attack(TaskScope scope) {
    Animation = AnimationDriver.Play(scope, WindupAnimation);
    HitConfig hitConfig = HitConfig;
    using var effect = Status.Add(ScriptedMove);
    await Animation.WaitDone()(scope);
    await Fall(scope);
    PhaseHits.Clear();
    var rotation = AbilityManager.transform.rotation;
    var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
    Animation = AnimationDriver.Play(scope, AttackAnimation);
    await scope.Any(Animation.WaitFrame(ActiveEnd.AnimFrames+1), Waiter.Repeat(OnHit(hitConfig)));
    Tags.AddFlags(AbilityTag.Cancellable);
    await Animation.WaitDone()(scope);
  }

  async Task Fall(TaskScope scope) {
    var cc = Status.GetComponent<CharacterController>();
    while (!cc.isGrounded) {
      await scope.Tick();
      Mover.Move(FallSpeed * Time.fixedDeltaTime * Vector3.down);
    }
  }

  TaskFunc OnHit(HitConfig hitConfig) => async (TaskScope scope) => {
    try {
      HitBox.enabled = true;
      var hitCount = await scope.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
      for (var i = 0; i < hitCount; i++) {
        var hit = Hits[i];
        if (!PhaseHits.Contains(hit) && hit.TryGetComponent(out Hurtbox hurtbox)) {
          hurtbox.TryAttack(new HitParams(hitConfig, Attributes));
          PhaseHits.Add(hit);
        }
      }
    } finally {
      HitBox.enabled = false;
    }
  };
}