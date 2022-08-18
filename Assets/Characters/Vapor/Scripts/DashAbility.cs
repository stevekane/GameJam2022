using System.Collections;
using UnityEngine;

public class DashAbility : Ability {
  [SerializeField] float MoveSpeedFactor = 3f;
  [SerializeField] ParticleSystem Particles;
  [SerializeField] AudioClip AudioClip;
  [SerializeField] float AudioClipStartingTime;
  Animator Animator;
  AudioSource AudioSource;

  public IEnumerator Begin() {
    Animator.SetBool("Dashing", true);
    AudioSource.Stop();
    AudioSource.clip = AudioClip;
    AudioSource.time = AudioClipStartingTime;
    AudioSource.Play();
    AddStatusEffect(new SpeedFactorEffect(MoveSpeedFactor, 1f));
    yield return Fiber.Any(Dashing(), Fiber.ListenFor(AbilityManager.GetEvent(Release)));
    Stop();
  }
  public IEnumerator Release() => null;

  IEnumerator Dashing() {
    Vector3 lastPosition = AbilityManager.transform.position;
    while (true) {
      Vector3 delta = (AbilityManager.transform.position - lastPosition) / Time.fixedDeltaTime;
      lastPosition = AbilityManager.transform.position;
      if (Particles != null)
        Particles.transform.forward = -delta.TryGetDirection() ?? -AbilityManager.transform.forward;
      yield return null;
    }
  }

  public override void Stop() {
    Animator.SetBool("Dashing", false);
    AudioSource.Stop();
    base.Stop();
  }

  void Awake() {
    //Controller = GetComponent<CharacterController>();
    Animator = GetComponentInParent<Animator>();
    AudioSource = GetComponentInParent<AudioSource>();
  }
}
