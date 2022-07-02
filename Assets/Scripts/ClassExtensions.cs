using System;
using UnityEngine;

public static class VectorExtensions {
  // Projects a Vector3 to the XZ plane. Used when we only care about horizontal movement.
  public static Vector3 XZ(this Vector3 v) {
    return new Vector3(v.x, 0, v.z);
  }

  public static Vector3? TryGetDirection(this Vector3 origin, Vector3 target) {
    var direction = (target-origin).normalized;
    return direction.magnitude > 0 ? direction : null;
  }

  public static bool TryGetDirection(this Vector3 origin, Vector3 target, out Vector3 direction) {
    direction = (target-origin).normalized;
    return direction.sqrMagnitude > 0;
  }
}

public static class NullableExtensions {
  public static T OrDefault<T>(this Nullable<T> nullable, T defaultValue) where T : struct {
    return nullable.HasValue ? nullable.Value : defaultValue;
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

public static class AudioSourceExtensions {
  public static bool PlayOptionalOneShot(this AudioSource source, AudioClip clip) {
    if (clip) {
      source.PlayOneShot(clip);
      return true;
    } else {
      return false;
    }
  }
}