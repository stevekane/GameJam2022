using UnityEngine;

public static class VectorExtensions {
  // Projects a Vector3 to the XZ plane. Used when we only care about horizontal movement.
  public static Vector3 XZ(this Vector3 v) {
    return new Vector3(v.x, 0, v.z);
  }
}

public static class MonoExtensions {
  // Returns the (first matching) requested component on this object, creating it first if necessary.
  public static T GetOrCreateComponent<T>(this MonoBehaviour self) where T : MonoBehaviour {
    if (self.TryGetComponent(out T t)) {
      return t;
    } else {
      return self.gameObject.AddComponent<T>();
    }
  }
}