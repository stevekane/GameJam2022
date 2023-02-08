using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(SpawnData))]
public class SpawnWaveRandom : SpawnWave {
  public List<Transform> PossibleLocations;
  public List<Mob> Mobs;
  SpawnData SpawnData;

  void Awake() {
    this.InitComponent(out SpawnData);
    Debug.Assert(Mobs.Count <= PossibleLocations.Count, "Need more spawn locations to host our mobs");
  }

  public override async Task Spawn(TaskScope scope, int wave) {
    UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);  // why do I need to call this EXACTLY HERE?
    var spawners = PossibleLocations.ToList();
    spawners.Shuffle();
    var tasks = Mobs.Select((mob, i) => SpawnData.Spawn(scope, spawners[i], mob, wave));
    await scope.AllTask(tasks.ToArray());
  }

#if UNITY_EDITOR
  [ContextMenu("Snap Locations to Ground")]
  public void SnapToGround() {
    foreach (var spawner in PossibleLocations) {
      if (Physics.Raycast(new Ray(spawner.position, Vector3.down), out var hit, float.MaxValue, Layers.EnvironmentMask))
        spawner.position = hit.point;
    }
  }
#endif

  void OnDrawGizmosSelected() {
    Gizmos.color = Color.red;
    foreach (var spawner in PossibleLocations) {
      Gizmos.DrawCube(spawner.position, Vector3.one);
    }
  }
}