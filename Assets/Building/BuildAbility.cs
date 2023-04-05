using System.Threading.Tasks;
using UnityEngine;

public class BuildAbility : Ability {
  [SerializeField] BuildObject BuildPrefab;
  [SerializeField] BuildGridCell GridCellPrefab;
  [SerializeField] Material GhostMaterial;
  BuildObject GhostInstance;
  bool IsBuildCellValid = false;

  BuildGrid Grid = new();

  public BuildObject SetBuildPrefab(BuildObject obj) => BuildPrefab = obj;

  public override bool CanStart(AbilityMethod func) => 0 switch {
    _ when func == AcceptAction => IsRunning,
    _ when func == AcceptRelease => IsRunning,
    _ when func == CancelAction => IsRunning,
    _ when func == RotateAction => IsRunning,
    _ => true
  };

  // TODO: better handling of distance (here and halfBuildSize)
  const float MaxBuildDist = 6f;
  public override async Task MainAction(TaskScope scope) {
    var debugThing = Instantiate(VFXManager.Instance.DebugIndicatorPrefab);
    var realMoveAxis = AbilityManager.CaptureAxis(AxisTag.Move);
    var realAimAxis = AbilityManager.CaptureAxis(AxisTag.Aim);
    try {
      var characterCell = BuildGrid.WorldToGrid(Character.transform.position);
      var yOffset = Character.transform.position.y;
      var halfBuildSize = (BuildPrefab.Size + new Vector2Int(6, 6)) / 2;  // rounds up
      var buildDir = Character.transform.forward.XZ2();
      var buildCell = Vector2Int.FloorToInt(characterCell + buildDir*halfBuildSize);
      var buildTarget = BuildGrid.GridToWorld(BuildPrefab, buildCell, yOffset);
      Grid.CreateGridCells(GridCellPrefab, characterCell, Character.transform.position.y);
      GhostInstance = Instantiate(BuildPrefab, buildTarget, Quaternion.identity);
      ApplyGhostMaterial(GhostInstance.gameObject);
      GhostInstance.gameObject.SetActive(true);
      Vector2Int? lastBuildCell = null;
      var which = await scope.Any(
        WaitForAccept,
        ListenFor(CancelAction),
        Waiter.Repeat(async s => {
          buildTarget += realMoveAxis.XZ * Mover.WalkSpeed * Time.fixedDeltaTime;
          var buildDelta = buildTarget - Character.transform.position;
          var moveAxis = new Vector3(
            Mathf.Abs(buildDelta.x) < MaxBuildDist ? 0 : realMoveAxis.XZ.x,
            0f,
            Mathf.Abs(buildDelta.z) < MaxBuildDist ? 0 : realMoveAxis.XZ.z);
          Mover.SetMoveAim(moveAxis, moveAxis);
          buildCell = BuildGrid.WorldToGrid(buildTarget);
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
      AbilityManager.UncaptureAxis(AxisTag.Move, realMoveAxis);
      AbilityManager.UncaptureAxis(AxisTag.Aim, realAimAxis);
      Destroy(GhostInstance.gameObject);
      Destroy(debugThing);
      Grid.Clear();
    }
  }

  public async Task WaitForAccept(TaskScope scope) {
    while (true) {
      if (AcceptHeld && IsBuildCellValid) {
        var center = BuildGrid.WorldToGrid(GhostInstance.transform.position);
        var (bottomLeft, topRight) = BuildGrid.GetBuildingBounds(BuildPrefab, center);
        //Debug.Log($"Placing {BuildPrefab} at {center} tr={GhostInstance.transform.position} bounds={bottomLeft}, {topRight}");
        var obj = Instantiate(BuildPrefab, GhostInstance.transform.position, GhostInstance.transform.rotation);
        obj.gameObject.SetActive(true);
        //FindObjectsOfType<Machine>().ForEach(m => m.UpdateOutputCells());
        if (!BuildPrefab.CanPlaceMultiple)
          break;
        Grid.RemoveCells(BuildPrefab, center);
        IsBuildCellValid = false;
      }
      await scope.Tick();
    }
  }

  bool AcceptHeld = false;
  public Task AcceptAction(TaskScope scope) { AcceptHeld = true; return null; }
  public Task AcceptRelease(TaskScope scope) { AcceptHeld = false; return null; }
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