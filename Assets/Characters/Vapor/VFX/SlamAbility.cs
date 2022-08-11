using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
  public HitParams HitParams;
  public GameObject FireVFX;
  public AudioClip FireSFX;
  public Vector3 FireVFXOffset;
  public GameObject HitVFX;
  public AudioClip HitSFX;

  void Start() {
    Owner = GetComponentInParent<AbilityManager>().transform;
    Animator = GetComponentInParent<Animator>();
  }

  public IEnumerator ChargeStart() {
    yield return MakeRoutine();
  }
  public IEnumerator ChargeRelease() {
    Windup.OnChargeEnd();
    yield return null;
  }

  protected override IEnumerator MakeRoutine() {
    yield return Fiber.Any(Charging(), Windup.StartWithCharge(Animator, Index));
    SlamAction.Activate();
    SFXManager.Instance.TryPlayOneShot(FireSFX);
    VFXManager.Instance.TrySpawnEffect(FireVFX, SlamAction.Piece.transform.position + FireVFXOffset);
    SlamAction = null;
    yield return Active.Start(Animator, Index);
    yield return Recovery.Start(Animator, Index);
    Done();
  }

  public void Done() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    if (SlamAction != null) {
      SlamAction.Activate();
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
    defender.OnHit(HitParams, attacker.transform);
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    VFXManager.Instance.TrySpawnEffect(HitVFX, defender.transform.position);
  }
}