using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate IEnumerator AbilityMethod();

public class SlamAbility : Ability {
  Animator Animator;
  public AnimationClip Clip;
  public Timeval WindupDuration;
  public Timeval SlamPiecePeriod;
  public GameObject SlamActionPrefab;
  SlamAction SlamAction;
  public HitConfig HitConfig;
  public GameObject FireVFX;
  public AudioClip FireSFX;
  public Vector3 FireVFXOffset;
  public float ChargeSpeedFactor = 1f/6f;

  AnimationTask Animation;

  void Start() {
    Animator = GetComponentInParent<Animator>();
  }

  public IEnumerator ChargeStart() {
    Animation = new AnimationTask(Animator, Clip);
    AddStatusEffect(new SpeedFactorEffect(.5f, .5f));
    Animation.SetSpeed(ChargeSpeedFactor);
    yield return Fiber.Any(Charging(), Animation.PlayUntil(WindupDuration.Ticks));
    Animation.SetSpeed(1f);
    SlamAction.Activate();
    SFXManager.Instance.TryPlayOneShot(FireSFX);
    VFXManager.Instance.TrySpawnEffect(FireVFX, SlamAction.Piece.transform.position + FireVFXOffset);
    SlamAction = null;
    yield return Animation;
  }

  public IEnumerator ChargeRelease() {
    Animation?.SetSpeed(1f);
    yield return null;
  }

  public override void OnStop() {
    Animation = null;
    if (SlamAction != null) {
      Destroy(SlamAction.gameObject);
      SlamAction = null;
    }
  }

  IEnumerator Charging() {
    int frames = 0;
    var slam = Instantiate(SlamActionPrefab, transform, false);
    slam.layer = gameObject.layer;
    SlamAction = slam.GetComponent<SlamAction>();
    SlamAction.OnHit = OnHit;
    while (true) {
      if (--frames <= 0) {
        SlamAction.AddPiece();
        frames = SlamPiecePeriod.Frames;
      }
      yield return null;
    }
  }

  void OnHit(Transform attacker, Defender defender) {
    defender.OnHit(HitConfig.ComputeParams(Attributes), attacker.transform);
  }
}