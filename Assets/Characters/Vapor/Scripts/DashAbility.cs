using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashEffect : StatusEffect {
  float Factor;
  public DashEffect(float factor) => Factor = factor;
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) {
    status.MoveSpeedFactor = Factor;
  }
}

public class DashAbility : Ability {
  [SerializeField] float MoveSpeedFactor = 3f;
  [SerializeField] ParticleSystem Particles;
  [SerializeField] AudioClip AudioClip;
  [SerializeField] float AudioClipStartingTime;
  Animator Animator;
  Status Status;
  AudioSource AudioSource;

  public IEnumerator Begin() {
    Animator.SetBool("Dashing", true);
    AudioSource.Stop();
    AudioSource.clip = AudioClip;
    AudioSource.time = AudioClipStartingTime;
    AudioSource.Play();
    Status.Add(new DashEffect(MoveSpeedFactor));
    yield return Fiber.Any(Dashing(), Fiber.ListenFor(AbilityManager.GetEvent(Release)));
    Stop();
  }
  public IEnumerator Release() => null;

  IEnumerator Dashing() {
    Vector3 lastPosition = AbilityManager.transform.position;
    while (true) {
      Vector3 delta = (AbilityManager.transform.position - lastPosition) / Time.fixedDeltaTime;
      lastPosition = AbilityManager.transform.position;
      Particles.transform.forward = -delta.TryGetDirection() ?? -AbilityManager.transform.forward;
      yield return null;
    }
  }

  public override void Stop() {
    Animator.SetBool("Dashing", false);
    AudioSource.Stop();
    Status.Remove(Status.Get<DashEffect>());
    base.Stop();
  }

  void Awake() {
    Status = GetComponentInParent<Status>();
    //Controller = GetComponent<CharacterController>();
    Animator = GetComponentInParent<Animator>();
    AudioSource = GetComponentInParent<AudioSource>();
  }
}
