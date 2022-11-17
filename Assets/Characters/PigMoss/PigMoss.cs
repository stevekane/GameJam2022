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
  public Timeval ChargeDelay;
  public Timeval FireDelay;
  public Vibrator Vibrator;
  public int Count;

  public override void OnStop() {

  }
  public override IEnumerator Routine() {
    Vibrator.Vibrate(Vector3.up, ChargeDelay.Ticks, .5f);
    yield return Fiber.Wait(ChargeDelay);
    var direction = Owner.forward.XZ();
    var rotationPerProjectile = Quaternion.Euler(0, 1/(float)Count*360, 0);
    for (var i = 0; i < Count; i++) {
      direction = rotationPerProjectile*direction;
      var rotation = Quaternion.LookRotation(direction, Vector3.up);
      var radius = 7;
      var position = Owner.position+radius*direction+Vector3.up;
      GameObject.Instantiate(ProjectilePrefab, position, rotation);
      yield return Fiber.Wait(FireDelay);
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
  void Start() => Behavior = new Fiber(Fiber.Repeat(MakeBehavior));
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

  IEnumerator MakeBehavior() {
    yield return Fiber.Any(AcquireTarget(), LookAround());
    if (Target) {
      var burst = new RadialBurst {
        AbilityManager = AbilityManager,
        Owner = transform,
        ProjectilePrefab = RadialBurstProjectilePrefab,
        ChargeDelay = Timeval.FromMillis(500),
        FireDelay = Timeval.FromMillis(50),
        Vibrator = Vibrator,
        Count = 16
      };
      AbilityManager.TryInvoke(burst.Routine); // TODO: awkward.
      yield return burst;
    } else {
      var deltaToCenter = CenterOfArena.position-transform.position.XZ();
      var toCenter = deltaToCenter.TryGetDirection() ?? transform.forward;
      Mover.UpdateAxes(AbilityManager, deltaToCenter, toCenter);
      yield return null;
    }
  }
}