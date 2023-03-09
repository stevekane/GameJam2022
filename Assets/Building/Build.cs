using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Build : Ability {
  [SerializeField] GameObject BuildPrefab;
  [SerializeField] Material GhostMaterial;
  GameObject GhostInstance;

  const float GridSize = 5f;
  public override bool CanStart(AbilityMethod func) => 0 switch {
    _ when func == AcceptAction => IsRunning,
    _ when func == CancelAction => IsRunning,
    _ when func == RotateAction => IsRunning,
    _ => true
  };

  public override async Task MainAction(TaskScope scope) {
    try {
      var characterGrid = SnapToGrid(Character.transform.position, GridSize);
      var buildDestination = SnapToGrid(characterGrid + Character.transform.forward.XZ()*GridSize, GridSize);
      var buildDelta = buildDestination - characterGrid;
      var buildDir = Character.transform.forward.XZ();
      GhostInstance = Instantiate(BuildPrefab, buildDestination, Quaternion.identity);
      ApplyGhostMaterial(GhostInstance);
      GhostInstance.SetActive(true);
      await scope.Any(
        ListenFor(AcceptAction),
        ListenFor(CancelAction),
        Waiter.Repeat(async s => {
          var characterGrid = SnapToGrid(Character.transform.position + buildDir*1f, GridSize);
          buildDestination = SnapToGrid(characterGrid + buildDelta, GridSize);
          //DebugUI.Log(this, $"build={BuildDestination} char={CharacterPosition} chargrid={characterGrid} buildDir={buildDir}");
          GhostInstance.transform.position = SnapToGrid(buildDestination, GridSize);
          await scope.Tick();
        }));
    } finally {
      Destroy(GhostInstance);
    }
  }

  void ApplyGhostMaterial(GameObject obj) {
    var renderers = obj.GetComponentsInChildren<MeshRenderer>();
    renderers.ForEach(r => r.material = GhostMaterial);
  }

  public Task AcceptAction(TaskScope scope) {
    var obj = Instantiate(BuildPrefab, GhostInstance.transform.position, GhostInstance.transform.rotation);
    obj.SetActive(true);
    return null;
  }
  public Task CancelAction(TaskScope scope) => null;
  public Task RotateAction(TaskScope scope) {
    GhostInstance.transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
    return null;
  }

  Vector3 SnapToGrid(Vector3 pos, float gridSize) {
    float Adjust(float f) => SnapToGrid(f, gridSize) + gridSize*.5f;
    return new(Adjust(pos.x), SnapToGrid(pos.y, gridSize), Adjust(pos.z));
  }

  float SnapToGrid(float f, float gridSize) => Mathf.Floor(f / gridSize) * gridSize;
}