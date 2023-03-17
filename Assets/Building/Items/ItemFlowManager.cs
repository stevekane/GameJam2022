using System.Collections.Generic;
using UnityEngine;

public class ItemFlowManager : MonoBehaviour {
  public static ItemFlowManager Instance;

  class ItemFlow {
    public BaseItem Item;
    public Vector2Int CurrentCell;
    public Vector2Int LastCell;
    public Vector3 LastWorld;
    public Vector3 CurrentWorld;
  }

  List<ItemFlow> Items = new();

  public bool IsCellOccupied(Vector2Int pos, int itemID) {
    foreach (var item in Items) {
      if (item.Item.ID == itemID && item.CurrentCell == pos)
        return true;
    }
    return false;
  }

  void MoveItems() {
    var occupiedCount = new Dictionary<Vector2Int, int>();
    foreach (var item in Items) {
      var nextCell = FindNextCell(item);
      if (nextCell != null) {
        item.LastCell = item.CurrentCell;
        item.CurrentCell = nextCell.Value;
      }
      occupiedCount[item.CurrentCell] = occupiedCount.GetOrAdd(item.CurrentCell) + 1;
    }
    var placedCount = new Dictionary<Vector2Int, int>();
    foreach (var item in Items) {
      var count = occupiedCount[item.CurrentCell];
      var placed = placedCount.GetOrAdd(item.CurrentCell);
      placedCount[item.CurrentCell]++;
      var worldPos = BuildGrid.GridToWorld(item.CurrentCell, item.Item.transform.position.y);
      if (count > 1) {
        const float radius = .5f;
        var angle = 2f*Mathf.PI * placed / (count+1);
        worldPos += new Vector3(Mathf.Cos(angle)*radius, 0f, Mathf.Sin(angle)*radius);
        //worldPos.y += placed;
      }
      item.LastWorld = item.Item.transform.position;
      item.CurrentWorld = worldPos;
    }
  }

  void ConsumeItems() {
    List<ItemFlow> Consumed = new();
    foreach (var item in Items) {
      var contents = BuildGrid.GetCellContents(item.CurrentCell, item.Item.transform.position.y);
      if (contents?.GetComponent<Machine>() is var machine && machine && machine.Consume(item.Item)) {
        Consumed.Add(item);
        Debug.Log($"{machine} consuming {item.Item} at {item.CurrentCell}");
        continue;
      }
    }
    Items.RemoveAll(i => Consumed.Contains(i));
  }

  void ProduceItems() {
    var machines = FindObjectsOfType<Machine>();
    foreach (var machine in machines) {
      if (machine.MaybeProduce(out var item, out var outputCell)) {
        Debug.Log($"{machine} producing {item} at {outputCell}");
        var startCell = BuildGrid.WorldToGrid(machine.transform.position);
        var worldPos = BuildGrid.GridToWorld(outputCell, machine.transform.position.y);
        Items.Add(new() { Item = item, CurrentCell = outputCell, LastCell = startCell, CurrentWorld = worldPos, LastWorld = machine.transform.position});
      }
    }
  }

  static Vector2Int[] Directions = new[] {
    Vector2Int.left,
    Vector2Int.up,
    Vector2Int.right,
    Vector2Int.down,
  };
  Vector2Int? FindNextCell(ItemFlow item) {
    Vector2Int? nextCell = null;
    for (var i = 0; i < 4; i++) {
      var cell = item.CurrentCell + Directions[i];
      var contents = BuildGrid.GetCellContents(cell, item.Item.transform.position.y);
      if (nextCell == null && cell != item.LastCell &&
        contents?.GetComponent<Capillary>() is var cap && cap &&
        !IsCellOccupied(cell, item.Item.ID)) {
        nextCell = cell;
      } else if (contents?.GetComponent<Machine>() is var machine && machine && machine.CanConsume(item.Item)) {
        return cell;
      }
    }
    return nextCell;
  }

  void AnimateItems() {
    foreach (var item in Items) {
      var fromPos = item.LastWorld;
      var toPos = item.CurrentWorld;
      item.Item.transform.position = Vector3.Lerp(fromPos, toPos, 1f - TicksRemaining/60f);
    }
  }

  int TicksRemaining = 60;
  void FixedUpdate() {
    AnimateItems();
    if (TicksRemaining-- < 0) {
      TicksRemaining = 60;
      ConsumeItems();
      MoveItems();
      ProduceItems();
    }
  }
}
