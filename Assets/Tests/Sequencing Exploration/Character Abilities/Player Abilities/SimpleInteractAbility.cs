using System.Threading.Tasks;
using UnityEngine;

public class SimpleInteractAbility : ClassicAbility {
  public AbilityAction Rotate;
  public AbilityAction Cancel;
  public AbilityAction Confirm;

  Collider Collider;

  void OnTriggerEnter(Collider c) {
    Collider = c;
    AbilityManager.AddTag(AbilityTag.Interact);
  }

  void OnTriggerExit(Collider c) {
    Collider = null;
    AbilityManager.RemoveTag(AbilityTag.Interact);
    Stop();
  }

  public override async Task MainAction(TaskScope scope) {
    try {
      Debug.Log("Interaction started");
      await HandleActions(scope);
    }
    finally {
      Debug.Log("Interaction stopped");
    }
  }

  async Task HandleActions(TaskScope scope) {
    await scope.Any(HandleRotate, HandleCancel, HandleConfirm);
  }

  async Task HandleRotate(TaskScope scope) {
    await Rotate.ListenFor(scope);
    var ticks = 60;
    var degreesPerTick = 90f/ticks;
    for (var i = 0; i < ticks; i++) {
      Collider.transform.RotateAround(Collider.transform.position, Collider.transform.up, degreesPerTick);
      await scope.Tick();
    }
    await HandleActions(scope);
  }

  async Task HandleCancel(TaskScope scope) {
    await Cancel.ListenFor(scope);
    Debug.Log("Canceled");
  }

  async Task HandleConfirm(TaskScope scope) {
    await Confirm.ListenFor(scope);
    Debug.Log("Confirmed");
  }
}