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
  public GameObject Projectile;
  public int Count;
  public AnimationClip Animation;
  public Timeval FireDelay;

  public override void OnStop() {

  }
  public override IEnumerator Routine() {
    Debug.Log("Radial BURST");
    yield return Fiber.Wait(Timeval.FromSeconds(2));
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
  [SerializeField] float EyeHeight;
  [SerializeField] float MaxTargetingDistance;

  IEnumerator Behavior;
  Transform Target;
  Mover Mover;
  Animator Animator;
  AbilityManager AbilityManager;

  void Awake() {
    Mover = GetComponent<Mover>();
    Animator = GetComponent<Animator>();
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

  /*
  If we are close to the edge, use ability to move us towards the center
  If we have a target, favor trying to attack it
  Otherwise, favor walking towards the center
  */
  IEnumerator MakeBehavior() {
    yield return Fiber.Any(AcquireTarget(), LookAround());
    if (Target) {
      var burst = new RadialBurst { AbilityManager = AbilityManager };
      AbilityManager.TryInvoke(burst.Routine); // TODO: This is awkward but doing it here for the sake of the family
      yield return burst;
    } else {
      var deltaToCenter = CenterOfArena.position-transform.position.XZ();
      var toCenter = deltaToCenter.TryGetDirection() ?? transform.forward;
      Mover.UpdateAxes(AbilityManager, deltaToCenter, toCenter);
      yield return null;
    }
  }
}