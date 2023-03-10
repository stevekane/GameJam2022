using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [Header("Input")]
  [SerializeField] InputManager InputManager;
  [SerializeField] float MovementSpeed = 10;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Attack")]
  [SerializeField] AnimationClip Clip;
  [SerializeField] float IdealStrikeDistance = 1;

  [Header("Components")]
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] MeleeAttackAbility ThreeHitComboAbility;

  [Header("State")]
  [SerializeField] TargetDummyController Target;

  HashSet<GameObject> Targets = new();
  TaskScope Scope;

  public PlayableGraph Graph;

  public float LocalTimeScale = 1;
  public int HitStopFramesRemaining;
  public bool HitboxStillActive = true;
  public int PhaseStartFrame;
  public int PhaseEndFrame;
  public int AttackFrame;

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Scope = new TaskScope();
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    var dt = LocalTimeScale * Time.fixedDeltaTime;
    var movementInput = InputManager.Axis(AxisCode.AxisLeft);
    var screenDirection = movementInput.XY;
    var movementMagnitude = screenDirection.magnitude;
    var camera = Camera.main; // TODO: slow way to access camera
    var worldSpaceDirection = camera.transform.TransformDirection(screenDirection);
    worldSpaceDirection.y = 0;
    worldSpaceDirection = worldSpaceDirection.normalized;
    // if (Phase == AttackPhase.None) {
    //   if (movementMagnitude > 0) {
    //     transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
    //   }
    //   Controller.Move(dt * movementMagnitude * MovementSpeed * worldSpaceDirection);
    // }

    if (HitStopFramesRemaining > 0) {
      LocalTimeScale = 0;
      HitStopFramesRemaining--;
    } else {
      LocalTimeScale = 1;
    }
    Graph.Evaluate(dt);
    FixedFrame++;
    FixedTick.Fire();
  }

  void OnAnimatorMove() {
    var dp = Animator.deltaPosition;
    // move to target
    // if (Phase == AttackPhase.Windup) {
    //   var phaseDuration = PhaseEndFrame-PhaseStartFrame;
    //   var phaseFraction = Mathf.InverseLerp(PhaseStartFrame, PhaseEndFrame, AttackFrame);
    //   var remainingFrames = PhaseEndFrame-AttackFrame+1;
    //   var toTarget = Target.transform.position-transform.position;
    //   var idealPosition = Target.transform.position-toTarget.normalized * IdealStrikeDistance;
    //   var toIdealPosition = idealPosition-transform.position;
    //   var toIdealPositionDelta = toIdealPosition / remainingFrames;
    //   dp = Vector3.Lerp(dp, toIdealPosition, phaseFraction);
    // }
    // turn to target
    // if (Phase == AttackPhase.Windup) {
    //   var phaseDuration = PhaseEndFrame-PhaseStartFrame;
    //   var phaseFraction = Mathf.InverseLerp(PhaseStartFrame, PhaseEndFrame, AttackFrame);
    //   var remainingFrames = PhaseEndFrame-AttackFrame+1;
    //   var toTarget = Target.transform.position-transform.position;
    //   var desiredRotation = Quaternion.LookRotation(toTarget.normalized, transform.up);
    //   transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, phaseFraction);
    // }
    // if (Phase != AttackPhase.None) {
    //   foreach (var target in Targets) {
    //     target.SendMessage("OnSynchronizedMove", dp);
    //   }
    // }
    Controller.Move(dp);
  }

  void OnHit(MeleeContact contact) {
    Targets.Add(contact.Hurtbox.Owner);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks;
    HitboxStillActive = false;
  }

  void OnBlocked(MeleeContact contact) {
    Targets.Add(contact.Hurtbox.Owner);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2;
    HitboxStillActive = false;
  }

  void OnParried(MeleeContact contact) {
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHurt(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2;
    HitboxStillActive = false;
    // TODO: I believe the OnPlayableDestroy hook needs to be used on the AnimatorGraph
    // behavior to stop the animation playing on the remote graph as well...
    // I'm not entirely sure though
    Scope.Dispose();
    Scope = new();
    Animator.SetTrigger("Parried");
  }

  void StartAttack() {
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    Scope.Start(ThreeHitComboAbility.Attack);
  }

  float BlendWeight(float blendInFraction, float blendOutFraction, float fraction) {
    if (blendOutFraction > 0 && fraction >= (1-blendOutFraction)) {
      return 1-(fraction-(1-blendOutFraction))/blendOutFraction;
    } else if (blendInFraction > 0 && fraction <= blendInFraction) {
      return fraction/blendInFraction;
    } else {
      return 1;
    }
  }
}