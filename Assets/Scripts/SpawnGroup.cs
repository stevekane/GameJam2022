using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SpawnGroup : MonoBehaviour {
  public async Task Spawn(TaskScope scope, int wave) {
    var spawners = GetComponentsInChildren<Spawner>();
    var tasks = spawners.Select(sp => sp.Spawn(scope, wave));
    await scope.AllTask(tasks.ToArray());
  }
}