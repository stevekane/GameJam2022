using System.Threading.Tasks;
using UnityEngine;

public class SpawnData : MonoBehaviour {
  public GameObject SpawningVFX;
  public GameObject DoneVFX;
  public Vector3 VFXOffset;
  public Timeval Delay;

  public async Task Spawn(TaskScope scope, Transform spawner, Mob mob, int wave) {
    var spawn = Instantiate(mob);
    spawn.Wave = wave;
    spawn.gameObject.SetActive(false);
    spawn.transform.SetParent(spawner, false);
    spawn.transform.SetPositionAndRotation(spawner.position, spawner.rotation);
    VFXManager.Instance.TrySpawnEffect(SpawningVFX, spawner.position + VFXOffset, SpawningVFX.transform.rotation, Delay.Seconds + .1f);
    await scope.Delay(Delay);
    VFXManager.Instance.TrySpawnEffect(DoneVFX, spawner.position + VFXOffset, DoneVFX.transform.rotation);
    spawn.gameObject.SetActive(true);
  }
}