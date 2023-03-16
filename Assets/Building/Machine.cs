using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BuildObject))]
public class Machine : MonoBehaviour {
  [HideInInspector] public BuildObject BuildObject;
  public BaseItem ConsumeItem;
  public BaseItem ProduceItem;

  int ConsumeCount = 0;  // TODO: producecount
  public bool CanConsume(BaseItem item) => ConsumeCount < 2 && ConsumeItem?.ID == item.ID;
  public bool Consume(BaseItem item) {
    if (CanConsume(item) is var can && can) {
      Destroy(item.gameObject);
      ConsumeCount++;
    }
    return can;
  }

  public bool CanProduce() => (!ConsumeItem || ConsumeCount > 0) && OutputCells.Count > 0;
  public bool MaybeProduce(out BaseItem item, out Vector2Int outputCell) {
    item = null;
    outputCell = default;
    if (CanProduce() && GetNextOutputCell() is var nextCell && nextCell != null) {
      item = Instantiate(ProduceItem, transform.position, Quaternion.identity);
      outputCell = nextCell.Value;
      if (ConsumeItem)
        ConsumeCount--;
      return true;
    }
    return false;
  }

  List<Vector2Int> OutputCells = new();
  int LastOutputCellIdx = -1;
  public void UpdateOutputCells() {
    LastOutputCellIdx = -1;
    OutputCells.Clear();
    if (!ProduceItem)
      return;

    var center = BuildGrid.WorldToGrid(transform.position);
    var (bottomLeft, topRight) = BuildGrid.GetBuildingBounds(BuildObject, center);
    var bottom = bottomLeft.y;
    var left = bottomLeft.x;
    var top = topRight.y;
    var right = topRight.x;
    // Bottom
    for (var cell = new Vector2Int(left, bottom-1); cell.x <= right; cell.x++) {
      var contents = BuildGrid.GetCellContents(cell, transform.position.y);
      if (contents?.GetComponent<Capillary>() is var cap && cap != null)
        OutputCells.Add(cell);
    }
    // Right
    for (var cell = new Vector2Int(right+1, bottom); cell.y <= top; cell.y++) {
      var contents = BuildGrid.GetCellContents(cell, transform.position.y);
      if (contents?.GetComponent<Capillary>() is var cap && cap != null)
        OutputCells.Add(cell);
    }
    // Top
    for (var cell = new Vector2Int(left, top+1); cell.x <= right; cell.x++) {
      var contents = BuildGrid.GetCellContents(cell, transform.position.y);
      if (contents?.GetComponent<Capillary>() is var cap && cap != null)
        OutputCells.Add(cell);
    }
    // Left
    for (var cell = new Vector2Int(left-1, bottom); cell.y <= top; cell.y++) {
      var contents = BuildGrid.GetCellContents(cell, transform.position.y);
      if (contents?.GetComponent<Capillary>() is var cap && cap != null)
        OutputCells.Add(cell);
    }
  }

  Vector2Int? GetNextOutputCell() {
    for (int i = 0; i < OutputCells.Count; i++) {
      var outputIdx = (LastOutputCellIdx + 1 + i) % OutputCells.Count;
      var cell = OutputCells[outputIdx];
      if (!ItemFlowManager.Instance.IsCellOccupied(cell, ProduceItem.ID)) {
        LastOutputCellIdx = outputIdx;
        return cell;
      }
      Debug.Log($"{this} skipping {i}th/{OutputCells.Count} output {LastOutputCellIdx} at {cell}, occupied");
    }
    Debug.Log($"{this} failed to output");
    return null;
  }

  void Awake() {
    this.InitComponent(out BuildObject);
  }
}