using System;
using System.Collections;
using UnityEngine;

abstract class PigMossAbility : IAbility, IEnumerator {
  public AbilityManager AbilityManager { get; set; }
  public Attributes Attributes { get => AbilityManager.GetComponent<Attributes>(); }
  public Status Status { get => AbilityManager.GetComponent<Status>(); }
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
  public override bool Equals(object obj) => base.Equals(obj);
  public override int GetHashCode() => base.GetHashCode();
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

class RadialBurst : PigMossAbility {
  public Transform Owner;
  public GameObject ProjectilePrefab;
  public AudioClip FireSFX;
  public Timeval ChargeDelay;
  public Timeval FireDelay;
  public Vibrator Vibrator;
  public int Count;
  public int Rotations;

  StatusEffect StatusEffect;

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
    Vibrator.Vibrate(Vector3.up, ChargeDelay.Ticks, 1f);
    yield return Fiber.Wait(ChargeDelay);
    var rotationPerProjectile = Quaternion.Euler(0, 360/(float)Count, 0);
    var halfRotationPerProjectile = Quaternion.Euler(0, 180/(float)Count, 0);
    var delay = FireDelay.Ticks;
    var direction = Owner.forward.XZ();
    for (var j = 0; j < Rotations; j++) {
      SFXManager.Instance.TryPlayOneShot(FireSFX);
      for (var i = 0; i < Count; i++) {
        direction = rotationPerProjectile*direction;
        var rotation = Quaternion.LookRotation(direction, Vector3.up);
        var radius = 5;
        var position = Owner.position+radius*direction+Vector3.up;
        GameObject.Instantiate(ProjectilePrefab, position, rotation);
      }
      yield return Fiber.Wait(FireDelay);
      direction = halfRotationPerProjectile*direction;
    }
  }
}

class BumRush : PigMossAbility {
  public override void OnStop() {

  }
  public override IEnumerator Routine() {
    yield return null;
  }
}

public class PigMoss : MonoBehaviour {
  [SerializeField] Transform CenterOfArena;
  [SerializeField] LayerMask TargetLayerMask;
  [SerializeField] LayerMask EnvironmentLayerMask;
  [SerializeField] GameObject RadialBurstProjectilePrefab;
  [SerializeField] AudioClip RadialBurstFireSFX;
  [SerializeField] float EyeHeight;
  [SerializeField] float MaxTargetingDistance;

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
    yield return Fiber.Wait(Timeval.FromSeconds(3));
    if (Target) {
      var burst = new RadialBurst {
        AbilityManager = AbilityManager,
        Owner = transform,
        ProjectilePrefab = RadialBurstProjectilePrefab,
        FireSFX = RadialBurstFireSFX,
        ChargeDelay = Timeval.FromMillis(1000),
        FireDelay = Timeval.FromMillis(250),
        Vibrator = Vibrator,
        Count = 16,
        Rotations = 5
      };
      AbilityManager.TryInvoke(burst.Routine); // TODO: awkward.
      yield return burst;
    } else {
      var deltaToCenter = CenterOfArena.position-transform.position.XZ();
      if (deltaToCenter.magnitude > 5) {
        var toCenter = deltaToCenter.TryGetDirection() ?? transform.forward;
        Mover.SetMove(AbilityManager, toCenter);
      } else {
        Mover.SetMove(AbilityManager, transform.forward);
      }
      yield return null;
    }
  }
}