using UnityEngine;

public class VFXManager : MonoBehaviour {
  public static VFXManager Instance;

  void Awake() {
    Instance = this;
  }

  void OnDestroy() {
    Instance = null;
  }

  public bool TrySpawnEffect(Camera camera, GameObject prefab, Vector3 position) {
    if (prefab) {
      var rotation = camera
        ? Quaternion.LookRotation(camera.transform.position)
        : Quaternion.identity;
      var effect = Instantiate(prefab, position, rotation);
      Destroy(effect, 3);
      return true;
    } else {
      return false;
    }
  }
}