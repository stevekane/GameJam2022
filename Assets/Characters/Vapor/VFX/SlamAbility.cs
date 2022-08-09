using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AbilityRunner {
  public string EventTag;
  EventSource Event;
  Func<bool> CanRun;
  System.Action DidRun;
  Func<IEnumerator> Routine;
  Fiber? Fiber;
  Ability Owner;

  public bool IsRunning { get => Fiber.HasValue && Owner.IsFiberRunning(Fiber.Value); }

  public void Init(AbilityUser user, Ability ability, Func<IEnumerator> routine, Func<bool> canRun) {
    Owner = ability;
    Routine = routine;
    AddRunCondition(canRun);
    Event = user.GetEvent(EventTag);
    Event.Action += EventAction;
    // TODO: unregister
  }

  public void Stop() {
    Owner.StopRoutine(Fiber.Value);
  }

  public void AddRunCondition(Func<bool> canRun) {
    var oldCanRun = CanRun;
    if (oldCanRun != null)
      CanRun = () => oldCanRun() && canRun();
    else
      CanRun = canRun;
  }

  public void AddDidRun(System.Action didRun) {
    DidRun += didRun;
  }

  void EventAction() {
    if (CanRun()) {
      Fiber = new Fiber(MakeRoutine());
      Owner.StartRoutine(Fiber.Value);
      //Debug.Log($"Fiber starting: {Fiber} {IsRunning}");
    }
  }

  IEnumerator MakeRoutine() {
    yield return Routine();
    DidRun?.Invoke();
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
    yield return MakeRoutine();
  }
  protected IEnumerator ChargeReleaseR() {
    Windup.OnChargeEnd();
    yield return null;
  }

  void Start() {
    Owner = GetComponentInParent<AbilityUser>().transform;
    Animator = GetComponentInParent<Animator>();
    AbilityUser = GetComponentInParent<AbilityUser>();
    ChargeStart.Init(AbilityUser, this, ChargeStartR, () => !ChargeStart.IsRunning);
    ChargeRelease.Init(AbilityUser, this, ChargeReleaseR, () => ChargeStart.IsRunning);
  }

  void OnHit(Transform attacker, Defender defender) {
    defender.OnHit(HitParams, attacker.transform);
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    VFXManager.Instance.TrySpawnEffect(HitVFX, defender.transform.position);
  }
}