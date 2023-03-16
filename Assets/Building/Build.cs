using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Build : Ability {
  [SerializeField] BuildObject BuildPrefab;
  [SerializeField] BuildGridCell GridCellPrefab;
  [SerializeField] Material GhostMaterial;
  BuildObject GhostInstance;
  bool IsBuildCellValid = false;

  BuildGrid Grid = new();

  public BuildObject SetBuildPrefab(BuildObject obj) => BuildPrefab = obj;

  public override bool CanStart(AbilityMethod func) => 0 switch {
    _ when func == AcceptAction => IsRunning,
    _ when func == CancelAction => IsRunning,
    _ when func == RotateAction => IsRunning,
    _ => true
  };

  public override async Task MainAction(TaskScope scope) {
    var debugThing = Instantiate(VFXManager.Instance.DebugIndicatorPrefab);
    try {
      var characterCell = BuildGrid.WorldToGrid(Character.transform.position);
      var halfBuildSize = (BuildPrefab.Size + new Vector2Int(6, 6)) / 2;  // rounds up
      var buildDir = Character.transform.forward.XZ2();
      var buildCell = Vector2Int.FloorToInt(characterCell + buildDir*halfBuildSize);
      var buildDelta = buildCell - characterCell;
      var yOffset = Character.transform.position.y;
      Grid.CreateGridCells(GridCellPrefab, characterCell, Character.transform.position.y);
      GhostInstance = Instantiate(BuildPrefab, BuildGrid.GridToWorld(BuildPrefab, buildCell, yOffset), Quaternion.identity);
      ApplyGhostMaterial(GhostInstance.gameObject);
      GhostInstance.gameObject.SetActive(true);
      Vector2Int? lastBuildCell = null;
      var which = await scope.Any(
        WaitForAccept,
        ListenFor(CancelAction),
        Waiter.Repeat(async s => {
          characterCell = BuildGrid.WorldToGrid(Character.transform.position);
          buildCell = Vector2Int.FloorToInt(characterCell + buildDir*halfBuildSize);
          if (lastBuildCell != buildCell) {
            IsBuildCellValid = Grid.IsValidBuildPos(BuildPrefab, buildCell);
            if (lastBuildCell.HasValue)
              Grid.UpdateCellState(BuildPrefab, lastBuildCell.Value, BuildGridCell.State.Empty);
            Grid.UpdateCellState(BuildPrefab, buildCell, IsBuildCellValid ? BuildGridCell.State.Valid : BuildGridCell.State.Invalid);
            lastBuildCell = buildCell;
          }
          //DebugUI.Log(this, $"build={BuildDestination} char={CharacterPosition} chargrid={characterGrid} buildDir={buildDir}");
          GhostInstance.transform.position = BuildGrid.GridToWorld(BuildPrefab, buildCell, yOffset);
          debugThing.transform.position = BuildGrid.GridToWorld(buildCell, yOffset);
          await scope.Tick();
        }));
    } finally {
      Destroy(GhostInstance.gameObject);
      Destroy(debugThing);
      Grid.Clear();
    }
  }

  public async Task WaitForAccept(TaskScope scope) {
    while (true) {
      await ListenFor(AcceptAction)(scope);
      if (IsBuildCellValid) {
        var center = BuildGrid.WorldToGrid(GhostInstance.transform.position);
        var (bottomLeft, topRight) = BuildGrid.GetBuildingBounds(BuildPrefab, center);
        Debug.Log($"Placing {BuildPrefab} at {center} tr={GhostInstance.transform.position} bounds={bottomLeft}, {topRight}");
        var obj = Instantiate(BuildPrefab, GhostInstance.transform.position, GhostInstance.transform.rotation);
        obj.gameObject.SetActive(true);
        FindObjectsOfType<Machine>().ForEach(m => m.UpdateOutputCells());
        if (!BuildPrefab.CanPlaceMultiple)
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

  void ApplyGhostMaterial(GameObject obj) {
    var renderers = obj.GetComponentsInChildren<MeshRenderer>();
    renderers.ForEach(r => r.material = GhostMaterial);
  }
}