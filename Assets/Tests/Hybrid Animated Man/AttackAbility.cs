using System;
using System.Collections;
using System.Collections.Generic;
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
  [SerializeField] Timeval WindupEnd;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] Animator Animator;
  [SerializeField] AnimationDriver AnimationDriver;
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent TriggerEvent;
  [SerializeField] Collider HitBox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;
  [SerializeField] GameObject AttackHitVFX;
  [SerializeField] AudioClip AttackHitSFX;

  HashSet<Collider> PhaseHits = new();
  Collider[] Hits = new Collider[16];
  AnimationJobFacade Animation;

  public override void OnStop() {
    HitBox.enabled = false;
    PhaseHits.Clear();
    Animation?.OnFrame.Unlisten(OnFrame);
  }

  public IEnumerator Attack() {
    var handleHits = Fiber.Repeat(OnHit);
    Animation = AnimationDriver.Play(AttackAnimation);
    Animation.OnFrame.Listen(OnFrame);
    yield return Fiber.Any(Animation, handleHits);
  }

  void OnFrame(int frame) {
    HitBox.enabled = frame >= WindupEnd.AnimFrames && frame <= ActiveEnd.AnimFrames;
    if (frame == WindupEnd.AnimFrames) {
      var position = AbilityManager.transform.position;
      var rotation = AbilityManager.transform.rotation;
      var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
      SFXManager.Instance.TryPlayOneShot(AttackSFX);
      VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
    }
  }

  IEnumerator OnHit() {
    var hitDetection = Fiber.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
    yield return hitDetection;
    var hitCount = hitDetection.Value;
    var attacker = AbilityManager.transform;
    var newHits = false;
    var hitParams = HitConfig.ComputeParams(Attributes);
    for (var i = 0; i < hitCount; i++) {
      var hit = Hits[i];
      var contact = hit.transform.position;
      var rotation = AbilityManager.transform.rotation;
      if (!PhaseHits.Contains(hit)) {
        VFXManager.Instance.TrySpawn2DEffect(AttackHitVFX, contact+Vector3.up, rotation);
        hit.GetComponent<Hurtbox>()?.Defender.OnHit(hitParams, attacker);
        PhaseHits.Add(hit);
        newHits = true;
      }
    }
    if (newHits) {
      SFXManager.Instance.TryPlayOneShot(AttackHitSFX);
      CameraShaker.Instance.Shake(HitConfig.CameraShakeStrength);
      yield return new HitStop(-transform.forward, hitParams.HitStopDuration, Status, Animator, AnimationDriver, Vibrator);
      Status.Add(new RecoilEffect(HitConfig.RecoilStrength * -attacker.forward));
    }
  }
}