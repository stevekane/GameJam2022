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
  [SerializeField] float HurtLeftStartTime;
  [SerializeField] float HurtRightStartTime;
  [SerializeField] float HurtForwardStartTime;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHurtVFX;
  [SerializeField] ParticleSystem KnockbackSmoke;
  [SerializeField] float KnockbackSmokeMinimumSpeed = 5;
  [SerializeField, Range(0,1)] float HitStopSpeed = .1f;

  [Header("Physics")]
  [SerializeField] Vector3 Gravity = new Vector3(0, -10, 0);
  [SerializeField] float Friction = .25f;

  [Header("Components")]
  [SerializeField] Animator Animator;
  [SerializeField] CharacterController Controller;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] SimpleFlash SimpleFlash;

  [Header("State")]
  [SerializeField] Vector3 Velocity;
  [SerializeField] float LocalTimeScale = 1;
  [SerializeField] float LocalAnimationTimeScale = 1;

  AnimatorControllerPlayable AnimatorControllerPlayable;
  PlayableGraph Graph;
  TaskScope Scope;

  int HitStopFramesRemaining;

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
    if (HitStopFramesRemaining > 0) {
      LocalTimeScale = 0;
      LocalAnimationTimeScale = Mathf.MoveTowards(LocalTimeScale, HitStopSpeed, .1f);
      HitStopFramesRemaining--;
    } else {
      LocalTimeScale = 1;
      LocalAnimationTimeScale = 1;
      HitStopFramesRemaining = 0;
    }
    var dt = Time.fixedDeltaTime * LocalTimeScale;
    var animDt = LocalAnimationTimeScale * Time.fixedDeltaTime;
    var verticalVelocity = dt * Gravity.y;
    if (Controller.isGrounded) {
      verticalVelocity += Controller.velocity.y;
    }
    var planarVelocity = Mathf.Exp(-dt * Friction) * Velocity;
    Velocity = new Vector3(planarVelocity.x, verticalVelocity, planarVelocity.z);
    if (Velocity.magnitude > KnockbackSmokeMinimumSpeed) {
      KnockbackSmoke.Emit(1);
    }
    Graph.Evaluate(animDt);
    Controller.Move(dt * Velocity);
  }

  void OnSynchronizedMove(Vector3 deltaPosition) {
    Controller.Move(deltaPosition);
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
    HitStopFramesRemaining = hitbox.HitStopDuration.Ticks;
    SimpleFlash.TicksRemaining = 20;
    Vibrator.VibrateOnHurt(vfxRotation * transform.forward, hitbox.HitStopDuration.Ticks);
    Animator.SetTrigger(hitbox.HitDirection switch {
      HitDirection.Left => "HurtLeft",
      HitDirection.Right => "HurtRight",
      _ => "HurtForward"
    });
    var sfxGameObject = new GameObject();
    var sfxSource = sfxGameObject.AddComponent<AudioSource>();
    sfxSource.playOnAwake = false;
    sfxSource.clip = hitbox.HitDirection switch {
      HitDirection.Left => HurtLeftSFX,
      HitDirection.Right => HurtRightSFX,
      _ => HurtForwardSFX
    };
    sfxSource.Play();
    sfxSource.time = hitbox.HitDirection switch {
      HitDirection.Left => HurtLeftStartTime,
      HitDirection.Right => HurtRightStartTime,
      _ => HurtForwardStartTime
    };
    Destroy(sfxGameObject, 3);
    Velocity += -transform.forward * hitbox.KnockbackStrength;
  }
}