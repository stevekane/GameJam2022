using UnityEngine;
using UnityEngine.Playables;

[DefaultExecutionOrder(ScriptExecutionGroups.Animation)]
public class LogicalTimeline : MonoBehaviour {
  [Header("Visual Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Components")]
  [SerializeField] Animator Animator;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  [Header("State")]
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Velocity Velocity;
  [SerializeField] HitStop HitStop;
  [SerializeField] GroundDistance GroundDistance;

  [Header("Hanging")]
  [Header("Configuration")]
  public float HangAngle;
  public Vector3 HangOffset = new Vector3(0, -2, -1);
  public Vector3 LeftHandHangOffset;
  public Vector3 RightHandHangOffset;
  [Header("State")]
  public Ledge Ledge;
  public bool Hanging;

  // TODO: Should not be here
  public PlayableGraph Graph;

  void Start() {
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void FixedUpdate() {
    // TODO: Should localtimescale affect basic inputs?
    // if (Hanging) {
    //   transform.forward = Ledge.Pivot.forward;
    //   transform.position = Ledge.Pivot.position + transform.TransformVector(HangOffset);
    // } else {

    // if (Hanging) {
    //   HandIKWeight = 1;
    //   Controller.enabled = false;
    // } else {
    //   HandIKWeight = 0;
    //   Controller.enabled = true;
    // }

    // UPDATE THE ANIMATOR (MOVE TO SEPARATE SYSTEM)
    // Steve - this rounding probably should be temporary and replaced / refactored with something
    // that is more robust. It exists to prevent the value being fed into the blend tree from stuttering
    // under floating-point imprecision which causes the animations to stutter awkwardly.
    // Animator.SetBool("Hanging", Hanging);
    Animator.SetFloat("Speed", Mathf.RoundToInt(Velocity.Value.XZ().magnitude));
    Animator.SetFloat("YSpeed", Velocity.Value.y);
    Animator.SetBool("Grounded", SimpleAbilityManager.Tags.Current.HasFlag(AbilityTag.Grounded));
    Animator.SetFloat("GroundDistance", GroundDistance.Value);
    Graph.Evaluate(LocalTime.FixedDeltaTime);
  }

  /*
  float HandIKWeight;

  void OnAnimatorIK() {
    Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, HandIKWeight);
    Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, HandIKWeight);
    Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, HandIKWeight);
    Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, HandIKWeight);
    if (Hanging) {
      var leftHand = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.LeftHand);
      var rightHand = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.RightHand);
      var leftHandTarget = Ledge.Pivot.position + transform.TransformVector(LeftHandHangOffset);
      var rightHandTarget = Ledge.Pivot.position + transform.TransformVector(RightHandHangOffset);
      Animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget);
      Animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget);
      Animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(transform.forward));
      Animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(transform.forward));
    }
  }

  void OnLedgeEnter(Ledge l) {
    Ledge = l;
  }

  void OnLedgeExit(Ledge l) {
    Ledge = null;
  }

  void OnLedgeStay(Ledge l) {
    if (!Hanging) {
      var validAngle = Vector3.Angle(transform.forward, l.Pivot.forward) <= HangAngle;
      var isFalling = Velocity.Value.y < -2;
      Hanging = validAngle && isFalling;
    }
  }
  */

  void OnHit(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks;
  }

  void OnBlocked(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2;
  }

  // TODO: Restore the idea of cancelling attack ability if parried
  void OnParried(MeleeContact contact) {
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHurt(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2;
    Animator.SetTrigger("Parried");
  }
}