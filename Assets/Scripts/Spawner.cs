using System.Threading.Tasks;
using UnityEngine;

public class Spawner : MonoBehaviour {
  public GameObject VFX;
  public Timeval Delay;

  public async Task Spawn(TaskScope scope, Mob mob, int wave) {
    var spawn = Instantiate(mob);
    spawn.Wave = wave;
    spawn.gameObject.SetActive(false);
    spawn.transform.SetParent(transform, false);
    spawn.transform.SetPositionAndRotation(transform.position, transform.rotation);
    VFXManager.Instance.TrySpawnEffect(VFX, transform.position, VFX.transform.rotation, Delay.Seconds);
    await scope.Delay(Delay);
    spawn.gameObject.SetActive(true);
  }
}