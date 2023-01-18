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
    var spawners = PossibleLocations.ToList();
    spawners.Shuffle();
    var tasks = Mobs.Select((mob, i) => SpawnData.Spawn(scope, spawners[i], mob, wave));
    await scope.AllTask(tasks.ToArray());
  }
}