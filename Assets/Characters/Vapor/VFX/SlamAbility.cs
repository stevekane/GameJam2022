using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

  protected override IEnumerator MakeRoutine() {
    Owner = GetComponentInParent<AbilityUser>().transform;
    Animator = GetComponentInParent<Animator>();
    yield return Fiber.Any(Charging(), Windup.StartWithCharge(Animator, Index));
    SlamAction.Activate();
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
}