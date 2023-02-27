using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class TargetDummyController : MonoBehaviour {
  [Header("Animation")]
  [SerializeField] RuntimeAnimatorController AnimatorController;

  [Header("Audio")]
  [SerializeField] AudioClip HurtLeftSFX;
  [SerializeField] AudioClip HurtRightSFX;
  [SerializeField] AudioClip HurtForwardSFX;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHurtVFX;
  [SerializeField] Timeval HitStopDuration = Timeval.FromAnimFrames(20, 60);
  [SerializeField, Range(0,1)] float HitStopSpeed = .1f;

  [Header("Components")]
  [SerializeField] Animator Animator;
  [SerializeField] CharacterController Controller;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Vibrator Vibrator;

  AnimatorControllerPlayable AnimatorControllerPlayable;
  PlayableGraph Graph;
  TaskScope Scope;
  Vector3 Velocity;
  Vector3 Acceleration;

  int HitStopFramesRemaining;
  float KnockbackTimeRemaining;
  float KnockbackStrength;

  void Start() {
    Scope = new();
    Graph = PlayableGraph.Create("Target Dummy");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    AnimatorControllerPlayable = AnimatorControllerPlayable.Create(Graph, AnimatorController);
    var output = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
    output.SetSourcePlayable(AnimatorControllerPlayable);
  }

  void OnDestroy() {
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    Graph.Evaluate(Time.fixedDeltaTime);
    if (HitStopFramesRemaining > 0) {
      AnimatorControllerPlayable.SetSpeed(HitStopSpeed);
      HitStopFramesRemaining--;
    } else {
      HitStopFramesRemaining = 0;
      AnimatorControllerPlayable.SetSpeed(1);
    }
    if (KnockbackTimeRemaining > 0) {
      // TODO: This is a pretty hacky and approximate way to get knockback to be
      // throttled by hitstop. There are def better ways...
      var dt = Time.fixedDeltaTime * (float)AnimatorControllerPlayable.GetSpeed();
      Controller.Move(dt * KnockbackStrength * -transform.forward);
      KnockbackTimeRemaining -= dt;
    } else {
      KnockbackTimeRemaining = 0;
    }
  }

  void OnHurt(Hitbox hitbox) {
    CameraShaker.Instance.Shake(hitbox.CameraShakeIntensity);
    var directionalRotation = hitbox.HitDirection switch {
      HitDirection.Left => Quaternion.Euler(0, -145, 0),
      HitDirection.Right => Quaternion.Euler(0, 145, 0),
      _ => Quaternion.Euler(0, 180, 0)
    };
    var vfxRotation = directionalRotation * transform.rotation;
    var vfx = Instantiate(OnHurtVFX, transform.position + Vector3.up, vfxRotation);
    Destroy(vfx, 3);
    HitStopFramesRemaining = HitStopDuration.Ticks;
    Vibrator.VibrateOnHurt(vfxRotation * transform.forward, HitStopDuration.Ticks);
    Animator.SetTrigger(hitbox.HitDirection switch {
      HitDirection.Left => "HurtLeft",
      HitDirection.Right => "HurtRight",
      _ => "HurtForward"
    });
    AudioSource.PlayOneShot(hitbox.HitDirection switch {
      HitDirection.Left => HurtLeftSFX,
      HitDirection.Right => HurtRightSFX,
      _ => HurtForwardSFX
    });
    KnockbackStrength = hitbox.KnockbackStrength;
    KnockbackTimeRemaining = hitbox.KnockbackDuration.Seconds;
  }
}