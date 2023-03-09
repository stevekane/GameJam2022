using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Build : Ability {
  [SerializeField] BuildObject BuildPrefab;
  [SerializeField] BuildGridCell GridCellPrefab;
  [SerializeField] Material GhostMaterial;
  BuildObject GhostInstance;
  bool IsBuildCellValid = false;
  const float GridSize = 1f;

  Dictionary<Vector2Int, BuildGridCell> Cells = new();

  public override bool CanStart(AbilityMethod func) => 0 switch {
    _ when func == AcceptAction => IsRunning,
    _ when func == CancelAction => IsRunning,
    _ when func == RotateAction => IsRunning,
    _ => true
  };

  public override async Task MainAction(TaskScope scope) {
    try {
      var characterCell = WorldToGrid(Character.transform.position);
      var halfBuildSize = (BuildPrefab.Size + new Vector2Int(6, 6)) / 2;  // rounds up
      var buildDir = Character.transform.forward.XZ2();
      var buildCell = Vector2Int.FloorToInt(characterCell + buildDir*halfBuildSize);
      var buildDelta = buildCell - characterCell;
      var yOffset = Character.transform.position.y;
      CreateGridCells(characterCell, Character.transform.position.y);
      GhostInstance = Instantiate(BuildPrefab, GridToWorld(buildCell, yOffset), Quaternion.identity);
      ApplyGhostMaterial(GhostInstance.gameObject);
      GhostInstance.gameObject.SetActive(true);
      Vector2Int? lastBuildCell = null;
      var which = await scope.Any(
        WaitForAccept,
        ListenFor(CancelAction),
        Waiter.Repeat(async s => {
          characterCell = WorldToGrid(Character.transform.position);
          buildCell = Vector2Int.FloorToInt(characterCell + buildDir*halfBuildSize);
          if (lastBuildCell != buildCell) {
            IsBuildCellValid = IsValidBuildPos(buildCell);
            if (lastBuildCell.HasValue)
              UpdateCellState(lastBuildCell.Value, BuildGridCell.State.Empty);
            UpdateCellState(buildCell, IsBuildCellValid ? BuildGridCell.State.Valid : BuildGridCell.State.Invalid);
            lastBuildCell = buildCell;
          }
          //DebugUI.Log(this, $"build={BuildDestination} char={CharacterPosition} chargrid={characterGrid} buildDir={buildDir}");
          GhostInstance.transform.position = GridToWorld(buildCell, yOffset);
          await scope.Tick();
        }));
    } finally {
      Destroy(GhostInstance.gameObject);
      Cells.ForEach(c => c.Value.gameObject.Destroy());
      Cells.Clear();
    }
  }

  public async Task WaitForAccept(TaskScope scope) {
    while (true) {
      await ListenFor(AcceptAction)(scope);
      if (IsBuildCellValid) {
        var obj = Instantiate(BuildPrefab, GhostInstance.transform.position, GhostInstance.transform.rotation);
        obj.gameObject.SetActive(true);
        break;
      }
    }
  }

  public Task AcceptAction(TaskScope scope) => null;
  public Task CancelAction(TaskScope scope) => null;
  public Task RotateAction(TaskScope scope) {
    GhostInstance.transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
    return null;
  }

  bool IsValidBuildPos(Vector2Int center) {
    var halfBuildSize = (BuildPrefab.Size + new Vector2Int(1, 1)) / 2;  // rounds up
    for (int x = center.x - halfBuildSize.x; x <= center.x + halfBuildSize.x; x++) {
      for (int y = center.y - halfBuildSize.y; y <= center.y + halfBuildSize.y; y++) {
        var pos = new Vector2Int(x, y);
        if (!Cells.ContainsKey(pos)) return false;
      }
    }
    return true;
  }

  void UpdateCellState(Vector2Int center, BuildGridCell.State state) {
    var halfBuildSize = (BuildPrefab.Size + new Vector2Int(1, 1)) / 2;  // rounds up
    for (int x = center.x - halfBuildSize.x; x <= center.x + halfBuildSize.x; x++) {
      for (int y = center.y - halfBuildSize.y; y <= center.y + halfBuildSize.y; y++) {
        var pos = new Vector2Int(x, y);
        if (Cells.TryGetValue(pos, out var c))
          c.SetState(state);
      }
    }
  }

  Vector2Int WorldToGrid(Vector3 worldPos) {
    worldPos *= 1f/GridSize;
    return new((int)worldPos.x, (int)worldPos.z);
  }

  Vector3 GridToWorld(Vector2Int gridPos, float yOffset = 0f) {
    return new(gridPos.x*GridSize + GridSize*.5f, yOffset, gridPos.y*GridSize + GridSize*.5f);
  }

  void CreateGridCells(Vector2Int center, float y) {
    var toVisit = new Queue<Vector2Int>();
    toVisit.Enqueue(center);
    while (toVisit.TryDequeue(out var pos)) {
      if (Cells.ContainsKey(pos))
        continue;
      var worldPos = GridToWorld(pos, y);
      const float epsilon = .01f;
      if (!Physics.Raycast(worldPos + Vector3.up*10f, Vector3.down, out var hit, 11f, Defaults.Instance.EnvironmentLayerMask)
        || hit.point.y > y+epsilon)
        continue;
      var indicator = Instantiate(GridCellPrefab, worldPos, Quaternion.identity);
      Cells.Add(pos, indicator);
      toVisit.Enqueue(pos + Vector2Int.left);
      toVisit.Enqueue(pos + Vector2Int.right);
      toVisit.Enqueue(pos + Vector2Int.up);
      toVisit.Enqueue(pos + Vector2Int.down);
    }
  }

  void ApplyGhostMaterial(GameObject obj) {
    var renderers = obj.GetComponentsInChildren<MeshRenderer>();
    renderers.ForEach(r => r.material = GhostMaterial);
  }
}