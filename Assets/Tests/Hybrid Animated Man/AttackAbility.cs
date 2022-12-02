using System;
using System.Collections;
using UnityEngine;

public abstract class CoroutineJob : IEnumerator, IStoppable {
  public IEnumerator Routine;
  public object Current => Routine?.Current;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (Routine != null) {
      if (Routine.MoveNext()) {
        return true;
      } else {
        Stop();
        return false;
      }
    } else {
      return false;
    }
  }
  public bool IsRunning => Routine != null;
  public void Stop() {
    Routine = null;
    OnStop();
  }
  public abstract void OnStop();
  public abstract IEnumerator MakeRoutine();
}

public class HitStop : CoroutineJob {
  public float Amplitude = .1f;
  public Vector3 Axis;
  public Timeval Duration;
  public Status Status;
  public Animator Animator;
  public AnimationDriver AnimationDriver;
  public Vibrator Vibrator;
  public HitStop(
  Vector3 axis,
  Timeval duration,
  Status status,
  Animator animator,
  AnimationDriver animationDriver,
  Vibrator vibrator) {
    Axis = axis;
    Duration = duration;
    Status = status;
    Animator = animator;
    AnimationDriver = animationDriver;
    Vibrator = vibrator;
    Routine = MakeRoutine();
  }
  public override void OnStop() {
    Status.CanMove = true;
    Status.CanRotate = true;
    Status.CanAttack = true;
    Animator.SetSpeed(1);
    AnimationDriver.Resume();
  }
  public override IEnumerator MakeRoutine() {
    yield return Fiber.Any(Fiber.Wait(Duration), Fiber.Repeat(OnTick));
  }
  void OnTick() {
    Status.CanMove = false;
    Status.CanRotate = false;
    Status.CanAttack = false;
    Animator.SetSpeed(0);
    AnimationDriver.Pause();
    Vibrator.VibrateThisFrame(Axis, Amplitude);
  }
}

public class AttackAbility : Ability {
  [SerializeField] float HitCameraShakeIntensity = 1f;
  [SerializeField] float HitRecoilStrength = 2f;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] Animator Animator;
  [SerializeField] AnimationDriver AnimationDriver;
  [SerializeField] PlayableAnimation AttackAnimation;
  [SerializeField] TriggerEvent TriggerEvent;
  [SerializeField] Collider HitBox;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  Collider[] Hits = new Collider[16];

  public override void OnStop() {
    HitBox.enabled = false;
  }

  public IEnumerator Attack() {
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, transform.position+Vector3.up, transform.rotation);
    var hitParams = HitConfig.ComputeParams(Attributes);
    var handleHits = Fiber.Repeat(OnHit, hitParams);
    var play = AnimationDriver.Play(AttackAnimation);
    HitBox.enabled = true;
    // TODO: This will NOT work for multiple targets currently. You need a ListenForMultiple or ListenForAll type of thing...
    // TODO: Atm this is subtely wrong. HandleHits gets canceled prematurely because AnimationJob ends first
    // This is another instance of the example in Core where two jobs must communicate through some shared state
    // to determine when they should terminate
    yield return Fiber.Any(play, handleHits);
  }

  IEnumerator OnHit(HitParams hitParams) {
    var hitDetection = Fiber.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
    yield return hitDetection;
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    var hitCount = hitDetection.Value;
    var attacker = AbilityManager.transform;
    for (var i = 0; i < hitCount; i++) {
      Hits[i].GetComponent<Hurtbox>()?.Defender.OnHit(hitParams, attacker);
    }
    yield return new HitStop(-transform.forward, hitParams.HitStopDuration, Status, Animator, AnimationDriver, Vibrator);
    // TODO: Upgrade the way Recoil Status is applied with new system
    Status.Add(new RecoilEffect(HitRecoilStrength * -attacker.forward));
    Stop();
  }
}