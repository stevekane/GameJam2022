using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class Container : MonoBehaviour, IInteractable {
  Inventory Inventory;

  // IInteractable
  public string[] Choices => new[] { "Deposit" };
  public void Choose(Character interacter, int choiceIdx) {
    var charInventory = interacter.GetComponent<Inventory>();
    charInventory.MoveTo(Inventory);
  }
  public void Rotate(float degrees) {
    transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
  }

  void Awake() {
    this.InitComponent(out Inventory);
  }
}