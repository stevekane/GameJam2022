using UnityEngine;

public class VFXManager : MonoBehaviour {
  public static VFXManager Instance;
  public Coin CoinPrefab;

  void Awake() {
    Instance = this;
  }

  public bool TrySpawnEffect(GameObject prefab, Vector3 position) {
    if (prefab) {
      var rotation = MainCamera.Instance
        ? Quaternion.LookRotation(MainCamera.Instance.transform.position)
        : Quaternion.identity;
      var effect = Instantiate(prefab, position, rotation);
      Destroy(effect, 3);
      return true;
    } else {
      return false;
    }
  }

  public void SpawnEffect(Effect effect, Vector3 position, Quaternion rotation) {
    Destroy(Instantiate(effect.GameObject, position, rotation), effect.Duration.Seconds);
  }
}