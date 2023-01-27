using System.Threading.Tasks;
using UnityEngine;

public class Spawner : SpawnData {
  public Mob Mob;

  public Task Spawn(TaskScope scope, int wave) => Spawn(scope, transform, Mob, wave);

#if UNITY_EDITOR
  [ContextMenu("Snap to Ground")]
  public void SnapToGround() {
    if (Physics.Raycast(new Ray(transform.position, Vector3.down), out var hit, float.MaxValue, Layers.EnvironmentMask))
      transform.position = hit.point;
  }
#endif
}