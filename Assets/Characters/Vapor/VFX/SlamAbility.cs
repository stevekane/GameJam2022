using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityRunner {
  public Func<bool> CanRun;
  public Func<IEnumerator> Routine;
  public EventSource Event;
  Fiber? Fiber;
  Ability Owner;

  public bool IsRunning { get {
      //Debug.Log($"Fiber isRunning: {Fiber} {Fiber != null} {Owner.IsFiberRunning(Fiber)}");
      return Fiber.HasValue && Owner.IsFiberRunning(Fiber.Value);
      //return Fiber != null && Owner.IsFiberRunning(Fiber);
    } }

  public void Init(Ability ability, Func<IEnumerator> routine, Func<bool> canRun) {
    Owner = ability;
    Routine = routine;
    CanRun = canRun;
    //Event.Action += () => {
    //  if (CanRun()) {
    //    Fiber = new Fiber(Routine());
    //    ability.StartRoutine(Fiber.Value);
    //  }
    //};
    Event.Action += EventAction;
  }

  public void Stop() {
    Owner.StopRoutine(Fiber.Value);
  }

  void EventAction() {
    if (CanRun()) {
      Fiber = new Fiber(Routine());
      Owner.StartRoutine(Fiber.Value);
      //Debug.Log($"Fiber starting: {Fiber} {IsRunning}");
    }
  }
}

public class SlamAbility : ChargedAbility {
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
  public AbilityRunner ChargeStart = new();
  public AbilityRunner ChargeRelease = new();
  AbilityUser AbilityUser;

  protected override IEnumerator MakeRoutine() {
    Owner = GetComponentInParent<AbilityUser>().transform;
    Animator = GetComponentInParent<Animator>();
    yield return Fiber.Any(Charging(), Windup.StartWithCharge(Animator, Index));
    SlamAction.Activate();
    SFXManager.Instance.TryPlayOneShot(FireSFX);
    VFXManager.Instance.TrySpawnEffect(FireVFX, SlamAction.Piece.transform.position + FireVFXOffset);
    SlamAction = null;
    yield return Active.Start(Animator, Index);
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public override void Stop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    if (SlamAction != null) {
      SlamAction.Activate();
      SlamAction = null;
    }
    base.Stop();
  }

  IEnumerator Charging() {
    int frames = 0;
    var slam = Instantiate(SlamActionPrefab, transform.position, transform.rotation);
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
 
  public override void ReleaseCharge() {
    Windup.OnChargeEnd();
  }
  protected IEnumerator ChargeStartR() {
    Debug.Log($"Start A {ChargeStart.IsRunning}");
    yield return MakeRoutine();
    Debug.Log($"Start B {ChargeStart.IsRunning}");
  }
  protected IEnumerator ChargeReleaseR() {
    Debug.Log($"Release A {ChargeStart.IsRunning}");
    Windup.OnChargeEnd();
    yield return null;
    Debug.Log($"Release B {ChargeStart.IsRunning}");
  }

  void Start() {
    Owner = GetComponentInParent<AbilityUser>().transform;
    Animator = GetComponentInParent<Animator>();
    AbilityUser = GetComponentInParent<AbilityUser>();
    ChargeStart.Event = AbilityUser.GetEvent("SlamStart");
    ChargeStart.Init(this, ChargeStartR, () => !ChargeStart.IsRunning);
    ChargeRelease.Event = AbilityUser.GetEvent("SlamRelease");
    ChargeRelease.Init(this, ChargeReleaseR, () => ChargeStart.IsRunning);
  }

  void OnHit(Transform attacker, Defender defender) {
    defender.OnHit(HitParams, attacker.transform);
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    VFXManager.Instance.TrySpawnEffect(HitVFX, defender.transform.position);
  }
}