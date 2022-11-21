using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PigMoss {
  [Serializable]
  class BuzzSawConfig {
    public AudioClip RevealedSFX;
    public AudioClip ExtendedSFX;
    public GameObject ExtendedVFX;
    public AudioClip HiddenSFX;
    public Animator Animator;
    public Timeval RevealedDuration;
    public Timeval ExtendedDuration;
    public TriggerEvent BladeTriggerEvent;
    public HitParams BladeHitParams;
  }

  class BuzzSaw : FiberAbility {
    enum BladeState { Hidden = 0, Revealed = 1, Extended = 2 }

    BuzzSawConfig Config;
    HashSet<Collider> Hits = new();

    public BuzzSaw(AbilityManager manager, BuzzSawConfig config) {
      AbilityManager = manager;
      Config = config;
    }
    public override void OnStop() {
      Config.Animator.SetInteger("State", (int)BladeState.Hidden);
    }
    public override IEnumerator Routine() {
      SFXManager.Instance.TryPlayOneShot(Config.RevealedSFX);
      Config.Animator.SetInteger("State", (int)BladeState.Revealed);
      yield return Fiber.Wait(Config.RevealedDuration);
      SFXManager.Instance.TryPlayOneShot(Config.ExtendedSFX);
      VFXManager.Instance.TrySpawn2DEffect(
        Config.ExtendedVFX,
        AbilityManager.transform.position,
        AbilityManager.transform.rotation,
        Config.ExtendedDuration.Seconds);
      Config.Animator.SetInteger("State", (int)BladeState.Extended);
      yield return Fiber.Any(Fiber.Wait(Config.ExtendedDuration), Fiber.Repeat(OnHit));
      Hits.ForEach(ProcessHit);
      SFXManager.Instance.TryPlayOneShot(Config.HiddenSFX);
      Config.Animator.SetInteger("State", (int)BladeState.Hidden);
    }
    IEnumerator OnHit() {
      var hitEvent = Fiber.ListenFor(Config.BladeTriggerEvent.OnTriggerStaySource);
      yield return hitEvent;
      var position = hitEvent.Value.transform.position;
      var direction = (position-AbilityManager.transform.position).XZ().normalized;
      var rotation = Quaternion.LookRotation(direction, Vector3.up);
      SFXManager.Instance.TryPlayOneShot(Config.BladeHitParams.SFX);
      VFXManager.Instance.TrySpawnEffect(Config.BladeHitParams.VFX, position, rotation);
      Hits.Add(hitEvent.Value);
    }
    void ProcessHit(Collider c) {
      if (c.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.Defender.OnHit(Config.BladeHitParams, AbilityManager.transform);
      }
    }
  }
}