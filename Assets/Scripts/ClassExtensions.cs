using System;
using System.Collections.Generic;
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

  public static Vector3? TryGetDirection(this Vector3 vector) {
    return vector.sqrMagnitude > 0 ? vector : null;
  }
}

public static class NullableExtensions {
  public static T OrDefault<T>(this Nullable<T> nullable, T defaultValue) where T : struct {
    return nullable.HasValue ? nullable.Value : defaultValue;
  }
}

public static class IntExtensions {
  public static int ScaleBy(this int i, float f) {
    return (int)((float)i*f);
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

  public static bool IsInFrontOf(this Vector3 p, Transform t) {
    var delta = p-t.position;
    var dot = Vector3.Dot(delta.normalized, t.forward);
    return dot >= 0;
  }

  public static bool IsVisibleFrom(this Transform t, Vector3 p, LayerMask layerMask) {
    var delta = t.position-p; 
    var direction = delta.normalized;
    var distance = delta.magnitude;
    var didHit = Physics.Raycast(p, direction, out RaycastHit hit, distance, layerMask);
    return didHit && hit.transform == t;
  }
}

public static class AudioSourceExtensions {
  public static bool PlayOptionalOneShot(this AudioSource source, AudioClip clip) {
    if (clip && source) {
      source.PlayOneShot(clip);
      return true;
    } else {
      return false;
    }
  }
}

public static class ArrayLikeExtensions {
  public static void ForEach<T>(this IEnumerable<T> xs, Action<T> p) {
    foreach (var x in xs) {
      p(x);
    }
  }

  public static Vector3 Sum<T>(this IEnumerable<T> xs, Func<T, Vector3> f) {
    var sum = Vector3.zero;
    foreach (var x in xs) {
      sum += f(x);
    }
    return sum;
  }

  public static bool TryGetIndex<T>(this T[] xs, int i, out T t) {
    var inBounds = i >= 0 && i < xs.Length;
    if (i >= 0 && i < xs.Length) {
      t = xs[i];
      return true;
    } else {
      t = default(T);
      return false;
    }
  }

  public static bool HasIndex<T>(this T[] xs, int i) {
    return i >= 0 && i < xs.Length;
  }

  public static int IndexOf<T>(this T[] xs, T t) {
    for (int i = 0; i < xs.Length; i++) {
      if (ReferenceEquals(xs[i],t)) {
        return i;
      }
    }
    return -1;
  }
}