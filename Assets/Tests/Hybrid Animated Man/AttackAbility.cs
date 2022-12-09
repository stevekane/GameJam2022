using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAbility : Ability {
  [SerializeField] Timeval WindupEnd;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] Animator Animator;
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent TriggerEvent;
  [SerializeField] Collider HitBox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] AnimationJobFacade Animation = null;

  public override void OnStop() {
    HitBox.enabled = false;
    PhaseHits.Clear();
    Animation?.OnFrame.Unlisten(OnFrame);
  }

  public IEnumerator Attack() {
    var handleHits = Fiber.Repeat(OnHit);
    Animation = AnimationDriver.Play(AttackAnimation);
    Animation.OnFrame.Listen(OnFrame);
    yield return Fiber.Any(Animation, handleHits);
  }

  void OnFrame(int frame) {
    HitBox.enabled = frame >= WindupEnd.AnimFrames && frame <= ActiveEnd.AnimFrames;
    if (frame == WindupEnd.AnimFrames) {
      var position = AbilityManager.transform.position;
      var rotation = AbilityManager.transform.rotation;
      var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
      SFXManager.Instance.TryPlayOneShot(AttackSFX);
      VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
    }
    if (frame >= ActiveEnd.AnimFrames) {
      CurrentTags.AddFlags(AbilityTag.Cancellable);
    }
  }

  IEnumerator OnHit() {
    var hitDetection = Fiber.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
    yield return hitDetection;
    var hitCount = hitDetection.Value;
    var attacker = AbilityManager.transform;
    for (var i = 0; i < hitCount; i++) {
      var hit = Hits[i];
      var contact = hit.transform.position;
      var rotation = AbilityManager.transform.rotation;
      if (!PhaseHits.Contains(hit) && hit.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject));
        PhaseHits.Add(hit);
      }
    }
  }
}