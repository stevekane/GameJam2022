using UnityEngine;

public class VFXManager : MonoBehaviour {
  public static VFXManager Instance;

  public Coin CoinPrefab;

  public bool TrySpawnEffect(GameObject prefab, Vector3 position, float lifetime = 3f) {
    var rotation = MainCamera.Instance
      ? Quaternion.LookRotation(MainCamera.Instance.transform.position)
      : Quaternion.identity;
    return TrySpawnEffect(prefab, position, rotation);
  }

  public bool TrySpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 3f) {
    if (prefab) {
      var effect = Instantiate(prefab, position, rotation);
      if (lifetime >= 0f)
        Destroy(effect, lifetime);
      return true;
    } else {
      return false;
    }
  }

  public bool TrySpawn2DEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 3f) {
    // This rotation trickery is an attempt to align a camera-facing spritesheet animation with the world-space orientation.
    // I don't know if this is correct. World-space y rotation correspends to a negative z rotation, I guess, but the x rotation
    // is black magic.
    return TrySpawnEffect(prefab, position, Quaternion.Euler(90, 0, -rotation.eulerAngles[1]), lifetime);
  }

  public void SpawnEffect(Effect effect, Vector3 position, Quaternion rotation) {
    Destroy(Instantiate(effect.GameObject, position, rotation), effect.Duration.Seconds);
  }
}