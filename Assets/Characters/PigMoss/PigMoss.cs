using System.Collections;
using UnityEngine;

abstract class PigMossAbility : IEnumerator, IStoppable {
  public IEnumerator Enumerator;
  public object Current { get => Enumerator.Current; }
  public bool MoveNext() => Enumerator.MoveNext();
  public void Dispose() => Enumerator = null;
  public void Reset() => Enumerator = Routine();
  public bool IsRunning { get; set; }
  public abstract void Stop();
  public abstract IEnumerator Routine();
}

class DesolateDive : PigMossAbility {
  public override void Stop() {
  }
  public override IEnumerator Routine() {
    yield return null;
  }
}

class RadialBurst : PigMossAbility {
  public override void Stop() {

  }
  public override IEnumerator Routine() {
    yield return null;
  }
}

class BumRush : PigMossAbility {
  public override void Stop() {

  }
  public override IEnumerator Routine() {
    yield return null;
  }
}

public class PigMoss : MonoBehaviour {
  [SerializeField] Transform CenterOfArena;

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

  /*
  If we are close to the edge, use ability to move us towards the center
  If we have a target, favor trying to attack it
  Otherwise, favor walking towards the center
  */
  IEnumerator MakeBehavior() {
    if (!Target) {
      var deltaToCenter = CenterOfArena.position-transform.position.XZ();
      var toCenter = deltaToCenter.TryGetDirection() ?? transform.forward;
      Debug.DrawRay(transform.position, toCenter);
      Mover.UpdateAxes(AbilityManager, deltaToCenter, toCenter);
      yield return null;
    }
  }
}