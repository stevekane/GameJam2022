using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Common interface for Containers, Crafters. Things that items can be put in/out of.
public interface IContainer {
  Transform Transform { get; }
  bool InsertItem(ItemProto item, int count = 1);
  bool ExtractItem(ItemProto item, int count = 1);
  int GetExtractCount(ItemProto item);
}

[RequireComponent(typeof(Inventory))]
public class Container : MonoBehaviour, IContainer, IInteractable {
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

  // IContainer
  public Transform Transform => transform;
  public bool InsertItem(ItemProto item, int count = 1) {
    Inventory.Add(item, count);
    WorkerManager.Instance.OnContainerChanged(this);
    return true;
  }
  public bool ExtractItem(ItemProto item, int count = 1) {
    if (GetExtractCount(item) >= count is var enough && enough)
      Inventory.Remove(item, count);
    return enough;
  }
  public int GetExtractCount(ItemProto item) => Inventory.Count(item);

  void Awake() {
    this.InitComponent(out Inventory);
  }

#if UNITY_EDITOR
  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    string ToString(Dictionary<ItemProto, int> queue) => string.Join("\n", queue.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"));
    GUIExtensions.DrawLabel(transform.position, ToString(Inventory.Contents));
  }
#endif
}