using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Build : Ability {
  [SerializeField] GameObject BuildPrefab;
  GameObject BuildInstance;
  Vector3 BuildDelta;
  Vector3 CharacterPosition;
  Vector3 BuildDestination;

  const float GridSize = 5f;
  public override async Task MainAction(TaskScope scope) {
    try {
      CharacterPosition = Character.transform.position;
      var characterGrid = AlignToGrid(Character.transform.position, GridSize);
      BuildDestination = AlignToGrid(characterGrid + Character.transform.forward.XZ()*GridSize, GridSize);
      BuildDelta = BuildDestination - characterGrid;
      var buildDir = (BuildDelta.TryGetDirection(characterGrid) ?? Character.transform.forward).XZ();
      buildDir = Character.transform.forward.XZ();
      var rotation = AlignToGrid(Character.transform.rotation);
      BuildInstance = Instantiate(BuildPrefab, BuildDestination, rotation);
      BuildInstance.SetActive(true);
      await scope.Any(
        Waiter.Repeat(async s => {
          var characterGrid = AlignToGrid(Character.transform.position + buildDir*1f, GridSize);
          BuildDestination = AlignToGrid(characterGrid + BuildDelta, GridSize);
          //DebugUI.Log(this, $"build={BuildDestination} char={CharacterPosition} chargrid={characterGrid} buildDir={buildDir}");
          BuildInstance.transform.position = AlignToGrid(BuildDestination, GridSize);
          await scope.Tick();
        }),
        ListenFor(MainRelease));
    } catch (OperationCanceledException) {
      Destroy(BuildInstance);
    } finally {
    }
  }

  Quaternion AlignToGrid(Quaternion rotation) {
    float Adjust(float f) => AlignToGrid(f, 90f);
    rotation.eulerAngles = new(Adjust(rotation.eulerAngles.x), Adjust(rotation.eulerAngles.y), Adjust(rotation.eulerAngles.z));
    return rotation;
  }

  Vector3 AlignToGrid(Vector3 pos, float gridSize) {
    float Adjust(float f) => AlignToGrid(f, gridSize) + gridSize*.5f;
    return new(Adjust(pos.x), AlignToGrid(pos.y, gridSize), Adjust(pos.z));
  }

  float AlignToGrid(float f, float gridSize) => Mathf.Floor(f / gridSize) * gridSize;
}