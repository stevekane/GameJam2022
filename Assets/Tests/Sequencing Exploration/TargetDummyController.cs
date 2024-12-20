using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Rendering.Universal;

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
  [SerializeField] AudioClip BlockSFX;
  [SerializeField] AudioClip ParrySFX;
  [SerializeField] AudioClip WallBounceSFX;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHurtVFX;
  [SerializeField] GameObject OnBlockVFX;
  [SerializeField] GameObject OnParryVFX;
  [SerializeField] ParticleSystem KnockbackSmoke;
  [SerializeField] ParticleSystem WallBounceDebris;
  [SerializeField] DecalProjector WallBounceDecal;
  [SerializeField] float KnockbackSmokeMinimumSpeed = 5;
  //[SerializeField, Range(0,1)] float HitStopSpeed = .1f;
  [SerializeField, ColorUsage(true, true)] Color HurtFlashColor = Color.red;
  [SerializeField, ColorUsage(true, true)] Color ParryFlashColor = Color.blue;
  [SerializeField, ColorUsage(true, true)] Color BlockFlashColor = Color.white;

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
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Velocity Velocity;
  [SerializeField] HitStop HitStop;
  [SerializeField] DefenderState State;

  AnimatorControllerPlayable AnimatorControllerPlayable;
  PlayableGraph Graph;
  TaskScope Scope;

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
    var dt = LocalTime.FixedDeltaTime;
    var verticalVelocity = dt * Gravity.y;
    if (Controller.isGrounded) {
      verticalVelocity += Controller.velocity.y;
    }
    var planarVelocity = Mathf.Exp(-dt * Friction) * Velocity.Value;
    Velocity.Value = new Vector3(planarVelocity.x, verticalVelocity, planarVelocity.z);
    if (Velocity.Value.magnitude > KnockbackSmokeMinimumSpeed) {
      KnockbackSmoke.Emit(1);
    }
    Animator.SetInteger("State", (int)State);
    Graph.Evaluate(dt);
    Controller.Move(dt * Velocity.Value);
  }

  void OnControllerColliderHit(ControllerColliderHit hit) {
    var collider = hit.collider;
    var velocity = Velocity.Value;
    var speed = velocity.magnitude;
    if (collider.gameObject.CompareTag("Wall") && speed > 20 && HitStop.TicksRemaining <= 0) {
      var centerPoint = hit.collider.ClosestPoint(transform.position + Vector3.up);
      var particleOrigin = centerPoint + hit.normal;
      var particleForward = Quaternion.LookRotation(hit.normal);
      var impactParticles = Instantiate(WallBounceDebris, particleOrigin, particleForward);
      Destroy(impactParticles.gameObject, 3);
      var decalOrigin = centerPoint + hit.normal * .1f;
      var decalForward = Quaternion.LookRotation(-hit.normal);
      var decal = Instantiate(WallBounceDecal, decalOrigin, decalForward);
      Destroy(decal.gameObject, 3);
      AudioSource.PlayOneShot(WallBounceSFX);
      Vibrator.Vibrate(hit.normal, 12, .1f);
      HitStop.TicksRemaining = 12;
      Animator.SetTrigger("HurtBack");
      CameraShaker.Instance.Shake(20);
      Velocity.Value = Vector3.Reflect(Velocity.Value / 2, hit.normal);
      transform.right = Vector3.Reflect(transform.right, hit.normal.XZ());
    }
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
    var forward = toAttacker.XZ().normalized.TryGetDirection() ?? transform.forward;
    transform.rotation = Quaternion.LookRotation(forward, transform.up);
    CameraShaker.Instance.Shake(hitbox.HitboxParams.CameraShakeIntensity);
    HitStop.TicksRemaining = hitbox.HitboxParams.HitStopTicks;
    SimpleFlash.FlashColor = HurtFlashColor;
    SimpleFlash.TicksRemaining = 20;
    Destroy(DirectionalVFX(transform, OnHurtVFX, hitbox.HitboxParams.HitDirection), 3);
    Vibrator.VibrateOnHurt(HitDirectionVector(transform, hitbox.HitboxParams.HitDirection), hitbox.HitboxParams.HitStopTicks);
    Animator.SetTrigger(hitbox.HitboxParams.HitDirection switch {
      HitDirection.Left => "HurtLeft",
      HitDirection.Right => "HurtRight",
      _ => "HurtFront"
    });
    var sfx = hitbox.HitboxParams.HitDirection switch {
      HitDirection.Left => HurtLeftSFX,
      HitDirection.Right => HurtRightSFX,
      _ => HurtForwardSFX
    };
    AudioSource.PlayOneShot(sfx);
    var damagePosition = transform.position + 2 * Vector3.up;
    var damageString = contact.Hitbox.HitboxParams.Damage.ToString();
    var damageMessage = WorldSpaceMessageManager.Instance.SpawnMessage(damageString, damagePosition);
    var damageVelocityWorldSpace = HitDirectionVector(transform, contact.Hitbox.HitboxParams.HitDirection);
    var damageVelocityLocalSpace = Camera.main.worldToCameraMatrix * damageVelocityWorldSpace;
    var isStrong = contact.Hitbox.HitboxParams.Damage > 100 || contact.Hitbox.HitboxParams.KnockbackStrength > 50;
    damageMessage.LocalVelocity = damageVelocityLocalSpace * (isStrong ? 15 : 10);
    damageMessage.LocalScale = (isStrong ? 1.5f : 1) * Vector3.one;
    Destroy(damageMessage.gameObject, 2);
    Velocity.Value += -transform.forward * hitbox.HitboxParams.KnockbackStrength;
  }

  void OnParry(MeleeContact contact) {
    var hitbox = contact.Hitbox;
    var hitParams = hitbox.HitboxParams;
    var toAttacker = hitbox.Owner.transform.position-transform.position;
    var forward = toAttacker.XZ().normalized.TryGetDirection() ?? transform.forward;
    transform.rotation = Quaternion.LookRotation(forward, transform.up);
    CameraShaker.Instance.Shake(hitParams.CameraShakeIntensity);
    HitStop.TicksRemaining = hitParams.HitStopTicks * 2;
    SimpleFlash.FlashColor = ParryFlashColor;
    SimpleFlash.TicksRemaining = 20;
    Destroy(DirectionalVFX(transform, OnParryVFX, hitParams.HitDirection), 3);
    Vibrator.VibrateOnHurt(HitDirectionVector(transform, hitParams.HitDirection), hitParams.HitStopTicks);
    // Animator.SetTrigger("Parry");
    AudioSource.PlayOneShot(ParrySFX);
  }

  void OnBlock(MeleeContact contact) {
    var hitbox = contact.Hitbox;
    var toAttacker = hitbox.Owner.transform.position-transform.position;
    var forward = toAttacker.XZ().normalized.TryGetDirection() ?? transform.forward;
    transform.rotation = Quaternion.LookRotation(forward, transform.up);
    CameraShaker.Instance.Shake(hitbox.HitboxParams.CameraShakeIntensity / 2);
    HitStop.TicksRemaining = hitbox.HitboxParams.HitStopTicks / 2;
    SimpleFlash.FlashColor = BlockFlashColor;
    SimpleFlash.TicksRemaining = 20;
    Destroy(DirectionalVFX(transform, OnBlockVFX, hitbox.HitboxParams.HitDirection), 3);
    Vibrator.VibrateOnHurt(HitDirectionVector(transform, hitbox.HitboxParams.HitDirection), hitbox.HitboxParams.HitStopTicks);
    Animator.SetTrigger("Block");
    AudioSource.PlayOneShot(BlockSFX);
  }

  Vector3 HitDirectionVector(Transform transform, HitDirection hitDirection) {
    return hitDirection switch {
      HitDirection.Left => Quaternion.Euler(0, -145, 0),
      HitDirection.Right => Quaternion.Euler(0, 145, 0),
      _ => Quaternion.Euler(0, 180, 0)
    } * transform.forward;
  }

  GameObject DirectionalVFX(Transform transform, GameObject prefab, HitDirection hitDirection) {
    var directionalRotation = hitDirection switch {
      HitDirection.Left => Quaternion.Euler(0, -145, 0),
      HitDirection.Right => Quaternion.Euler(0, 145, 0),
      _ => Quaternion.Euler(0, 180, 0)
    };
    var vfxRotation = directionalRotation * transform.rotation;
    return Instantiate(prefab, transform.position + Vector3.up, vfxRotation);
  }
}