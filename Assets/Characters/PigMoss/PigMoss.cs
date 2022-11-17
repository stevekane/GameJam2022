using System;
using System.Collections;
using UnityEngine;

abstract class PigMossAbility : IAbility, IEnumerator {
  public AbilityManager AbilityManager { get; set; }
  public Attributes Attributes { get => AbilityManager.GetComponent<Attributes>(); }
  public Status Status { get => AbilityManager.GetComponent<Status>(); }
  public Mover Mover { get => AbilityManager.GetComponent<Mover>(); }
  public AbilityTag Tags { get; set; }
  public void StartRoutine(Fiber routine) => Enumerator = routine;
  public void StopRoutine(Fiber routine) => Enumerator = null;
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerCondition.Empty;
  public object Current { get => Enumerator.Current; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (Enumerator != null && !Enumerator.MoveNext()) {
      Stop();
      return false;
    } else {
      return true;
    }
  }
  public void Stop() {
    Enumerator = null;
    OnStop();
  }
  public bool IsRunning { get; set; }
  public abstract void OnStop();
  public IEnumerator Enumerator;
  public abstract IEnumerator Routine();
}

class DesolateDive : PigMossAbility {
  public override void OnStop() {
  }
  public override IEnumerator Routine() {
    yield return null;
  }
}

class Bombard : PigMossAbility {
  public override void OnStop() {
  }
  public override IEnumerator Routine() {
    yield return null;
  }
}

[Serializable]
class RadialBurstConfig {
  public Transform Owner;
  public Vibrator Vibrator;
  public GameObject ProjectilePrefab;
  public AudioClip FireSFX;
  public Timeval ChargeDelay;
  public Timeval FireDelay;
  public int Count;
  public int Rotations;
}

class RadialBurst : PigMossAbility {
  RadialBurstConfig Config;
  StatusEffect StatusEffect;

  public RadialBurst(AbilityManager manager, RadialBurstConfig config) {
    AbilityManager = manager;
    Config = config;
  }

  public override void OnStop() {
    if (StatusEffect != null) {
      Status.Remove(StatusEffect);
    }
    StatusEffect = null;
  }
  public override IEnumerator Routine() {
    StatusEffect = new InlineEffect(status => {
      status.CanMove = false;
      status.CanRotate = false;
    });
    Status.Add(StatusEffect);
    Config.Vibrator.Vibrate(Vector3.up, Config.ChargeDelay.Ticks, 1f);
    yield return Fiber.Wait(Config.ChargeDelay);
    var rotationPerProjectile = Quaternion.Euler(0, 360/(float)Config.Count, 0);
    var halfRotationPerProjectile = Quaternion.Euler(0, 180/(float)Config.Count, 0);
    var delay = Config.FireDelay.Ticks;
    var direction = Config.Owner.forward.XZ();
    for (var j = 0; j < Config.Rotations; j++) {
      SFXManager.Instance.TryPlayOneShot(Config.FireSFX);
      for (var i = 0; i < Config.Count; i++) {
        direction = rotationPerProjectile*direction;
        var rotation = Quaternion.LookRotation(direction, Vector3.up);
        var radius = 5;
        var position = Config.Owner.position+radius*direction+Vector3.up;
        GameObject.Instantiate(Config.ProjectilePrefab, position, rotation);
      }
      yield return Fiber.Wait(Config.FireDelay);
      direction = halfRotationPerProjectile*direction;
    }
  }
}

[Serializable]
class BumRushConfig {
  public Animator Animator;
  public Timeval WindupDuration = Timeval.FromSeconds(1);
  public Timeval RushDuration = Timeval.FromSeconds(.5f);
  public Timeval RecoveryDuration = Timeval.FromSeconds(.5f);
  public TriggerEvent SpikeTriggerEvent;
  public HitParams SpikeHitParams;
  public float RushSpeed = 100;
}

class BumRush : PigMossAbility {
  new AbilityTag Tags = AbilityTag.Uninterruptible;

  BumRushConfig Config;
  Transform Target;
  StatusEffect RushStatusEffect;

  public BumRush(AbilityManager manager, BumRushConfig config, Transform target) {
    AbilityManager = manager;
    Config = config;
    Target = target;
  }
  public override void OnStop() {
    Status.Remove(RushStatusEffect);
    Config.Animator.SetBool("Extended", false);
  }
  IEnumerator Rush() {
    RushStatusEffect = new ScriptedMovementEffect();
    Status.Add(RushStatusEffect);
    var delta = Target.position-Mover.transform.position;
    var direction = delta.normalized;
    for (var tick = 0; tick < Config.RushDuration.Ticks; tick++) {
      Status.Move(direction*Config.RushSpeed*Time.fixedDeltaTime);
      yield return null;
    }
    Status.Remove(RushStatusEffect);
  }
  public override IEnumerator Routine() {
    Config.Animator.SetBool("Extended", true);
    yield return Fiber.Wait(Config.WindupDuration);
    var rush = Rush();
    var contact = Fiber.ListenFor(Config.SpikeTriggerEvent.OnTriggerEnterSource);
    var outcome = Fiber.Select(contact, rush);
    yield return outcome;
    // hit target
    if (outcome.Value == 0 && contact.Value.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.Defender.OnHit(Config.SpikeHitParams, AbilityManager.transform);
    }
    yield return Fiber.Wait(Config.RecoveryDuration);
    Config.Animator.SetBool("Extended", false);
  }
}

public class PigMoss : MonoBehaviour {
  [SerializeField] LayerMask TargetLayerMask;
  [SerializeField] LayerMask EnvironmentLayerMask;
  [Header("Radial Burst")]
  [SerializeField] RadialBurstConfig RadialBurstConfig;
  [Header("Bum Rush")]
  [SerializeField] BumRushConfig BumRushConfig;
  [Header("Targeting")]
  [SerializeField] float EyeHeight;
  [SerializeField] float MaxTargetingDistance;
  [Header("Navigation")]
  [SerializeField] Transform CenterOfArena;

  IEnumerator Behavior;
  Transform Target;
  Mover Mover;
  Animator Animator;
  Vibrator Vibrator;
  AbilityManager AbilityManager;

  void Awake() {
    Mover = GetComponent<Mover>();
    Animator = GetComponent<Animator>();
    Vibrator = GetComponent<Vibrator>();
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => Behavior = new Fiber(Fiber.All(Fiber.Repeat(MakeBehavior), Fiber.Repeat(LookAtTarget)));
  void OnDestroy() => Behavior = null;
  void FixedUpdate() => Behavior?.MoveNext();

  IEnumerator AcquireTarget() {
    Target = null;
    while (!Target) {
      var visibleTargetCount = PhysicsBuffers.VisibleTargets(
        position: transform.position+EyeHeight*Vector3.up,
        forward: transform.forward,
        fieldOfView: 180,
        maxDistance: MaxTargetingDistance,
        targetLayerMask: TargetLayerMask,
        targetQueryTriggerInteraction: QueryTriggerInteraction.Collide,
        visibleTargetLayerMask: TargetLayerMask | EnvironmentLayerMask,
        visibleQueryTriggerInteraction: QueryTriggerInteraction.Collide,
        buffer: PhysicsBuffers.Colliders);
      Target = visibleTargetCount > 0 ? PhysicsBuffers.Colliders[0].transform : null;
      yield return null;
    }
  }

  IEnumerator LookAround() {
    var randXZ = UnityEngine.Random.insideUnitCircle;
    var direction = new Vector3(randXZ.x, 0, randXZ.y);
    Mover.GetAxes(AbilityManager, out var move, out var forward);
    Mover.UpdateAxes(AbilityManager, move, direction);
    yield return Fiber.Wait(Timeval.FixedUpdatePerSecond);
  }

  void LookAtTarget() {
    Target = FindObjectOfType<Player>().transform;
    if (Target) {
      Mover.SetAim(AbilityManager, (Target.position-transform.position).normalized);
    }
  }

  IEnumerator MakeBehavior() {
    Target = FindObjectOfType<Player>().transform;
    var bumRush = new BumRush(AbilityManager, BumRushConfig, Target);
    AbilityManager.TryInvoke(bumRush.Routine);
    yield return bumRush;
    var burst = new RadialBurst(AbilityManager, RadialBurstConfig);
    AbilityManager.TryInvoke(burst.Routine); // TODO: awkward.
    yield return burst;
  }
}