using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate IEnumerator AbilityMethod();

public class SlamAbility : Ability {
  public int Index;
  public Transform Owner;
  public Animator Animator;
  public ChargedAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public Timeval SlamPiecePeriod;
  public GameObject SlamActionPrefab;
  SlamAction SlamAction;
  public HitConfig HitConfig;
  public GameObject FireVFX;
  public AudioClip FireSFX;
  public Vector3 FireVFXOffset;

  void Start() {
    Owner = GetComponentInParent<AbilityManager>().transform;
    Animator = GetComponentInParent<Animator>();
  }

  public IEnumerator ChargeStart() {
    AddStatusEffect(new SpeedFactorEffect(.5f, .5f));
    yield return Fiber.Any(Charging(), Windup.StartWithCharge(Animator, Index), Fiber.ListenFor(AbilityManager.GetEvent(ChargeRelease)));
    SlamAction.Activate();
    SFXManager.Instance.TryPlayOneShot(FireSFX);
    VFXManager.Instance.TrySpawnEffect(FireVFX, SlamAction.Piece.transform.position + FireVFXOffset);
    SlamAction = null;
    yield return Active.Start(Animator, Index);
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public IEnumerator ChargeRelease() => null;

  public override void Stop() {
    base.Stop();
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
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