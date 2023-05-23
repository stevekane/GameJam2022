using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class Container : MonoBehaviour, IInteractable {
  public Inventory Inventory;

  // IInteractable
  public string[] Choices => new[] { "Deposit" };
  public void Choose(Character interacter, int choiceIdx) {
    var charInventory = interacter.GetComponent<Inventory>();
    charInventory.MoveTo(Inventory);
  }
  public void Rotate(float degrees) {
    transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
  }

  public void Add(ItemInfo item, int count = 1) {
    Inventory.Add(item, count);
    WorkerManager.Instance.OnContainerChanged(this);
  }
  public void Remove(ItemInfo item, int count = 1) {
    Inventory.Remove(item, count);
  }

  void Awake() {
    this.InitComponent(out Inventory);
  }

#if UNITY_EDITOR
  public string[] DebugContents;
  void FixedUpdate() {
    IEnumerable<string> ToList(Dictionary<ItemInfo, int> queue) {
      foreach ((var item, int amount) in queue) {
        if (amount > 0)
          yield return $"{item.name}:{amount}";
      }
    }
    DebugContents = ToList(Inventory.Contents).ToArray();
  }

  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    string ToString(string[] list) => string.Join("\n", list);
    GUIExtensions.DrawLabel(transform.position, ToString(DebugContents));
  }
#endif
}