using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class BuildAbility : Ability {
  [SerializeField] BuildObject BuildPrefab;
  [SerializeField] BuildGridCell GridCellPrefab;
  [SerializeField] Material GhostMaterial;
  GameObject IndicatorInstance;
  bool IsBuildCellValid = false;

  BuildGrid Grid = new();

  bool IsDeleteMode => BuildPrefab == null;
  bool CanPlaceMultiple => BuildPrefab?.CanPlaceMultiple ?? true;
  public Vector2Int BuildingSize => BuildPrefab?.Size ?? Vector2Int.one;
  public BuildObject SetBuildPrefab(BuildObject obj) => BuildPrefab = obj;
  public BuildObject SetDeleteMode() => BuildPrefab = null;

  public override bool CanStart(AbilityMethod func) => 0 switch {
    _ when func == AcceptAction => IsRunning,
    _ when func == AcceptRelease => IsRunning,
    _ when func == CancelAction => IsRunning,
    _ when func == RotateAction => IsRunning,
    _ => true
  };

  // TODO: better handling of distance (here and halfBuildSize)
  const float MaxBuildDistInner = 6f;
  const float MaxBuildDistOuter = 7f;
  public override async Task MainAction(TaskScope scope) {
    AcceptHeld = false;
    var debugThing = Instantiate(VFXManager.Instance.DebugIndicatorPrefab);
    var realMoveAxis = AbilityManager.CaptureAxis(AxisTag.Move);
    var realAimAxis = AbilityManager.CaptureAxis(AxisTag.Aim);
    try {
      var characterCell = BuildGrid.WorldToGrid(Character.transform.position);
      var yOffset = Character.transform.position.y;
      var buildDir = Character.transform.forward.XZ2();
      var buildCell = Vector2Int.FloorToInt(characterCell + buildDir*MaxBuildDistInner);
      var buildTarget = BuildGrid.GridToWorld(BuildingSize, buildCell, yOffset);
      Grid.CreateGridCells(GridCellPrefab, characterCell, Character.transform.position.y);
      if (IsDeleteMode) {
        IndicatorInstance = debugThing;
      } else {
        IndicatorInstance = Instantiate(BuildPrefab, buildTarget, Quaternion.identity).gameObject;
        ApplyGhostMaterial(IndicatorInstance);
      }
      IndicatorInstance.SetActive(true);
      Vector2Int? lastBuildCell = null;
      var which = await scope.Any(
        WaitForAccept,
        ListenFor(CancelAction),
        Waiter.Repeat(async s => {
          var buildDelta = buildTarget - Character.transform.position;
          var movingAway = Vector3.Dot(realMoveAxis.XZ, buildDelta) >= 0f;
          var maxDist = movingAway ? MaxBuildDistOuter : MaxBuildDistInner;
          bool TooFar(float maxDist) => buildDelta.sqrMagnitude > maxDist.Sqr();
          var speed = Mover.WalkSpeed * (TooFar(MaxBuildDistOuter) && movingAway ? 1f : 2f);
          var moveAxis = TooFar(MaxBuildDistInner) && movingAway ? realMoveAxis.XZ : Vector3.zero;
          buildTarget += realMoveAxis.XZ * speed * Time.fixedDeltaTime;
          Mover.SetMoveAim(moveAxis, moveAxis);
          buildCell = BuildGrid.WorldToGrid(buildTarget);
          if (lastBuildCell != buildCell) {
            if (IsDeleteMode) {
              IsBuildCellValid = BuildGrid.GetCellContents(buildTarget) != null;
            } else {
              IsBuildCellValid = Grid.IsValidBuildPos(BuildingSize, buildCell);
            }
            if (lastBuildCell.HasValue)
              Grid.UpdateCellState(BuildingSize, lastBuildCell.Value, BuildGridCell.State.Empty);
            Grid.UpdateCellState(BuildingSize, buildCell, IsBuildCellValid ? BuildGridCell.State.Valid : BuildGridCell.State.Invalid);
            lastBuildCell = buildCell;
          }
          //DebugUI.Log(this, $"build={BuildDestination} char={CharacterPosition} chargrid={characterGrid} buildDir={buildDir}");
          IndicatorInstance.transform.position = BuildGrid.GridToWorld(BuildingSize, buildCell, yOffset);
          debugThing.transform.position = BuildGrid.GridToWorld(buildCell, yOffset);
          await scope.Tick();
        }));
    } finally {
      AbilityManager.UncaptureAxis(AxisTag.Move, realMoveAxis);
      AbilityManager.UncaptureAxis(AxisTag.Aim, realAimAxis);
      Destroy(IndicatorInstance);
      Destroy(debugThing);
      Grid.Clear();
    }
  }

  public async Task WaitForAccept(TaskScope scope) {
    while (true) {
      if (AcceptHeld && IsBuildCellValid) {
        var center = BuildGrid.WorldToGrid(IndicatorInstance.transform.position);
        if (IsDeleteMode) {
          var target = BuildGrid.GetCellContents(IndicatorInstance.transform.position);
          Destroy(target);
        } else {
          //var (bottomLeft, topRight) = BuildGrid.GetBuildingBounds(BuildPrefab, center);
          //Debug.Log($"Placing {BuildPrefab} at {center} tr={GhostInstance.transform.position} bounds={bottomLeft}, {topRight}");
          var obj = Instantiate(BuildPrefab, IndicatorInstance.transform.position, IndicatorInstance.transform.rotation);
          obj.gameObject.SetActive(true);
        }
        //FindObjectsOfType<Machine>().ForEach(m => m.UpdateOutputCells());
        if (!CanPlaceMultiple)
          break;
        if (IsDeleteMode) {
          Grid.UpdateCellState(BuildingSize, center, BuildGridCell.State.Empty);
        } else {
          Grid.RemoveCells(BuildingSize, center);
        }
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
    IndicatorInstance.transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
    return null;
  }

  void ApplyGhostMaterial(GameObject obj) {
    var renderers = obj.GetComponentsInChildren<MeshRenderer>();
    renderers.ForEach(r => r.material = GhostMaterial);
  }
}