using System.Linq;
using System.Threading.Tasks;

public class SpawnWaveChildren : SpawnWave {
  public override async Task Spawn(TaskScope scope, int wave) {
    var spawners = GetComponentsInChildren<Spawner>();
    var tasks = spawners.Select(sp => sp.Spawn(scope, wave));
    await scope.AllTask(tasks.ToArray());
  }
}