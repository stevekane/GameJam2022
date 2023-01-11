using System.Threading.Tasks;
using UnityEngine;

public class Spawner : MonoBehaviour {
  public Mob Mob;
  public GameObject VFX;
  public Vector3 VFXOffset;
  public Timeval Delay;

  public async Task Spawn(TaskScope scope, int wave) {
    var spawn = Instantiate(Mob);
    spawn.Wave = wave;
    spawn.gameObject.SetActive(false);
    spawn.transform.SetParent(transform, false);
    spawn.transform.SetPositionAndRotation(transform.position, transform.rotation);
    VFXManager.Instance.TrySpawnEffect(VFX, transform.position + VFXOffset, VFX.transform.rotation, Delay.Seconds);
    await scope.Delay(Delay);
    spawn.gameObject.SetActive(true);
  }
}