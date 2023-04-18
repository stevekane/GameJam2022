using System.Threading.Tasks;
using UnityEngine;

/*
When you are able to interact you will be granted new actions:

  Rotate
  Confirm
  Cancel

*/
public class SimpleInteractAbility : ClassicAbility {
  public AbilityAction Rotate;
  public AbilityAction Cancel;
  public AbilityAction Confirm;

  void OnTriggerEnter(Collider c) {
    AbilityManager.AddTag(AbilityTag.Interact);
  }

  // TODO: Think it might make sense to Stop the ability here?
  void OnTriggerExit(Collider c) {
    AbilityManager.RemoveTag(AbilityTag.Interact);
    Stop();
  }

  public override async Task MainAction(TaskScope scope) {
    await scope.Any(HandleRotate, HandleCancel, HandleConfirm);
  }

  async Task HandleRotate(TaskScope scope) {
    await Rotate.ListenFor(scope);
    Debug.Log("Start Rotating");
    await scope.Ticks(60);
    Debug.Log("Stop Rotating");
    await MainAction(scope);
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