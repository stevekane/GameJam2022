using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public enum DefenderState {
  Vulnerable,
  Blocking,
  Parrying
}

public class TargetDummyController : MonoBehaviour {
  [Header("Animation")]
  [SerializeField] RuntimeAnimatorController AnimatorController;

  [Header("Audio")]
  [SerializeField] AudioClip HurtLeftSFX;
  [SerializeField] AudioClip HurtRightSFX;
  [SerializeField] AudioClip HurtForwardSFX;

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
  [SerializeField] DefenderState State;

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

  void OnContact(MeleeContact contact) {
    switch (State) {
      case DefenderState.Vulnerable:
        OnHurt(contact);
        contact.Hitbox.Owner?.SendMessage("OnHit", contact);
      break;
      case DefenderState.Blocking:
        OnBlock(contact);
        contact.Hitbox.Owner?.SendMessage("OnBlocked", contact);
      break;
      case DefenderState.Parrying:
        OnParry(contact);
        contact.Hitbox.Owner?.SendMessage("OnParried", contact);
      break;
    }
  }

  void OnHurt(MeleeContact contact) {
    var hitbox = contact.Hitbox;
    var toAttacker = hitbox.Owner.transform.position-transform.position;
    transform.rotation = Quaternion.LookRotation(toAttacker, transform.up);
    CameraShaker.Instance.Shake(hitbox.CameraShakeIntensity);
    HitStopFramesRemaining = hitbox.HitStopDuration.Ticks;
    SimpleFlash.TicksRemaining = 20;
    Destroy(DirectionalVFX(transform, OnHurtVFX, hitbox.HitDirection), 3);
    Vibrator.VibrateOnHurt(DirectionalVibration(transform, hitbox.HitDirection), hitbox.HitStopDuration.Ticks);
    Animator.SetTrigger(hitbox.HitDirection switch {
      HitDirection.Left => "HurtLeft",
      HitDirection.Right => "HurtRight",
      _ => "HurtForward"
    });
    var sfx = hitbox.HitDirection switch {
      HitDirection.Left => HurtLeftSFX,
      HitDirection.Right => HurtRightSFX,
      _ => HurtForwardSFX
    };
    AudioSource.PlayOneShot(sfx);
    Velocity += -transform.forward * hitbox.KnockbackStrength;
  }

  void OnParry(MeleeContact contact) {
    Debug.Log("Parry");
  }

  void OnBlock(MeleeContact contact) {
    Debug.Log("Block");
  }

  GameObject DirectionalVFX(Transform transform, GameObject prefab, HitDirection hitDirection) {
    var directionalRotation = hitDirection switch {
      HitDirection.Left => Quaternion.Euler(0, -145, 0),
      HitDirection.Right => Quaternion.Euler(0, 145, 0),
      _ => Quaternion.Euler(0, 180, 0)
    };
    var vfxRotation = directionalRotation * transform.rotation;
    return Instantiate(OnHurtVFX, transform.position + Vector3.up, vfxRotation);
  }

  Vector3 DirectionalVibration(Transform transform, HitDirection hitDirection) {
    return hitDirection switch {
      HitDirection.Left => Quaternion.Euler(0, -145, 0),
      HitDirection.Right => Quaternion.Euler(0, 145, 0),
      _ => Quaternion.Euler(0, 180, 0)
    } * transform.forward;
  }
}