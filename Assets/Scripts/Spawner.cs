using System.Threading.Tasks;
using UnityEngine;

public class Spawner : MonoBehaviour {
  public Mob Mob;
  public GameObject SpawningVFX;
  public GameObject DoneVFX;
  public Vector3 VFXOffset;
  public Timeval Delay;

  public async Task Spawn(TaskScope scope, int wave) {
    var spawn = Instantiate(Mob);
    spawn.Wave = wave;
    spawn.gameObject.SetActive(false);
    spawn.transform.SetParent(transform, false);
    spawn.transform.SetPositionAndRotation(transform.position, transform.rotation);
    VFXManager.Instance.TrySpawnEffect(SpawningVFX, transform.position + VFXOffset, SpawningVFX.transform.rotation, Delay.Seconds + .1f);
    await scope.Delay(Delay);
    VFXManager.Instance.TrySpawnEffect(DoneVFX, transform.position + VFXOffset, DoneVFX.transform.rotation);
    spawn.gameObject.SetActive(true);
  }
}