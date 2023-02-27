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

  [Header("Components")]
  [SerializeField] Animator Animator;
  [SerializeField] CharacterController Controller;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Vibrator Vibrator;

  PlayableGraph Graph;
  TaskScope Scope;

  int KnockbackFramesRemaining;
  float KnockbackStrength;

  void Start() {
    Scope = new();
    Graph = PlayableGraph.Create("Target Dummy");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    var animatorController = AnimatorControllerPlayable.Create(Graph, AnimatorController);
    var output = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
    output.SetSourcePlayable(animatorController);
  }

  void OnDestroy() {
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    Graph.Evaluate(Time.fixedDeltaTime);
    if (KnockbackFramesRemaining > 0) {
      Controller.Move(Time.fixedDeltaTime * KnockbackStrength * -transform.forward);
      KnockbackFramesRemaining--;
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
    Vibrator.VibrateOnHurt(vfxRotation * transform.forward, 10);
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
    KnockbackFramesRemaining = hitbox.KnockbackDuration.Ticks;
  }
}