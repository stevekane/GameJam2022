using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildGrid {
  const float GridSize = 1f;

  Dictionary<Vector2Int, BuildGridCell> Cells = new();

  public static Vector2Int WorldToGrid(Vector3 worldPos) {
    worldPos *= 1f/GridSize;
    return new(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z));
  }

  public static Vector3 GridToWorld(Vector2Int gridPos, float yOffset = 0f) {
    return new(gridPos.x*GridSize + GridSize*.5f, yOffset, gridPos.y*GridSize + GridSize*.5f);
  }

  public static Vector3 GridToWorld(BuildObject building, Vector2Int gridPos, float yOffset = 0f) {
    // Bleh.. offset the building center by .5 for even sized buildings.
    var buildOffset = new Vector3((building.Size.x+1) % 2, 0f, (building.Size.y+1) % 2) * .5f;
    return new Vector3(gridPos.x*GridSize + GridSize*.5f, yOffset, gridPos.y*GridSize + GridSize*.5f) - buildOffset;
  }

  public static bool HasEmptyGround(Vector3 worldPos) {
    const float epsilon = .01f;
    //if (Physics.Raycast(worldPos + Vector3.up*10f, Vector3.down, out var hitx, 11f, Defaults.Instance.BuildingLayerMask, QueryTriggerInteraction.Collide))
    //  if (hitx.transform.gameObject.tag != "Ground")
    //    Debug.Log($"Hit {hitx.transform.parent} at {hitx.point}");
    return Physics.Raycast(worldPos + Vector3.up*10f, Vector3.down, out var hit, 11f, Defaults.Instance.BuildingLayerMask, QueryTriggerInteraction.Collide)
      && hit.point.y < worldPos.y+epsilon;
  }

  public static GameObject GetCellContents(Vector3 worldPos) {
    if (Physics.Raycast(worldPos + Vector3.up*10f, Vector3.down, out var hit, 11f, Defaults.Instance.BuildingLayerMask, QueryTriggerInteraction.Collide))
      return hit.transform.GetComponentInParent<BuildObject>()?.gameObject;
    return null;
  }

  public static GameObject GetCellContents(Vector2Int pos, float y) => GetCellContents(GridToWorld(pos, y));

  static public (Vector2Int, Vector2Int) GetBuildingBounds(BuildObject building, Vector2Int center) {
    var offsetBottomLeft = (building.Size) / 2;
    var offsetTopRight = (building.Size - Vector2Int.one) / 2;
    return (center - offsetBottomLeft, center + offsetTopRight);
  }

  // TODO: creating cells dynamically is slow. Cache this?
  public void CreateGridCells(BuildGridCell prefab, Vector2Int center, float y) {
    var toVisit = new Queue<Vector2Int>();
    toVisit.Enqueue(center);
    var invalidCells = new HashSet<Vector2Int>();
    while (toVisit.TryDequeue(out var pos)) {
      if (Cells.ContainsKey(pos) || invalidCells.Contains(pos))
        continue;
      var worldPos = GridToWorld(pos, y);
      if (!HasEmptyGround(worldPos)) {
        invalidCells.Add(pos);
        continue;
      }
      var indicator = GameObject.Instantiate(prefab, worldPos, Quaternion.identity);
      Cells.Add(pos, indicator);
      toVisit.Enqueue(pos + Vector2Int.left);
      toVisit.Enqueue(pos + Vector2Int.right);
      toVisit.Enqueue(pos + Vector2Int.up);
      toVisit.Enqueue(pos + Vector2Int.down);
    }
  }

  public void Clear() {
    Cells.ForEach(c => c.Value.gameObject.Destroy());
    Cells.Clear();
  }

  public bool IsValidBuildPos(BuildObject building, Vector2Int center) {
    var (bottomLeft, topRight) = GetBuildingBounds(building, center);
    foreach (var pos in CellsInSquare(bottomLeft, topRight)) {
      if (!Cells.ContainsKey(pos)) return false;
    }
    return true;
  }

  public void UpdateCellState(BuildObject building, Vector2Int center, BuildGridCell.State state) {
    var (bottomLeft, topRight) = GetBuildingBounds(building, center);
    foreach (var pos in CellsInSquare(bottomLeft, topRight)) {
      if (Cells.TryGetValue(pos, out var c))
        c.SetState(state);
    }
  }

  public void RemoveCells(BuildObject building, Vector2Int center) {
    var (bottomLeft, topRight) = GetBuildingBounds(building, center);
    foreach (var pos in CellsInSquare(bottomLeft, topRight)) {
      Cells.Remove(pos);
    }
  }

  IEnumerable<Vector2Int> CellsInSquare(Vector2Int bottomLeft, Vector2Int topRight) {
    Vector2Int current = new();
    for (current.x = bottomLeft.x; current.x <= topRight.x; current.x++) {
      for (current.y = bottomLeft.y; current.y <= topRight.y; current.y++) {
        yield return current;
      }
    }
  }
}

public class BuildGridCell : MonoBehaviour {
  public enum State { Valid, Invalid, Empty };

  [SerializeField] MeshRenderer Surface;
  [SerializeField] MeshRenderer SurfaceOuter;
  Color InnerColor;
  Color OuterColor;

  Color AsValid(Color c) => new(0f, c.g, c.b, 1f);
  Color AsInvalid(Color c) => new(c.g, 0f, c.b, 1f);
  Color AsEmpty(Color c) => new(c.r, c.g, c.b, 0f);

  public void SetState(State state) {
    Func<Color, Color> changeColor = state switch {
      State.Valid => AsValid,
      State.Invalid => AsInvalid,
      State.Empty => AsEmpty,
      _ => AsEmpty,
    };
    Surface.material.color = changeColor(InnerColor);
    SurfaceOuter.material.color = changeColor(OuterColor);
  }

  void Awake() {
    InnerColor = Surface.sharedMaterial.color;
    OuterColor = SurfaceOuter.sharedMaterial.color;
    SetState(State.Empty);
  }
}