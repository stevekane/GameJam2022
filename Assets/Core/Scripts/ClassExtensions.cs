using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public static class AnimationExtensions {
  public static bool TryAddLayerMaskFromAvatarMask(
    this AnimationLayerMixerPlayable layerMixer,
    int layerIndex,
    AvatarMask mask) {
      if (mask) {
        layerMixer.SetLayerMaskFromAvatarMask((uint)layerIndex, mask);
        return true;
      } else {
        return false;
      }
    }
}

public static class InputActionExtensions {
  public static void SetEnabled(this InputAction inputAction, bool enabled) {
    if (enabled && !inputAction.enabled)
      inputAction.Enable();
    else if (!enabled && inputAction.enabled)
      inputAction.Disable();
  }
  public static void SetEnabled(this InputActionMap inputActionMap, bool enabled) {
    if (enabled && !inputActionMap.enabled)
      inputActionMap.Enable();
    else if (!enabled && inputActionMap.enabled)
      inputActionMap.Disable();
  }
}

public static class VectorExtensions {
  // Projects a Vector3 to the XZ plane. Used when we only care about horizontal movement.
  public static Vector2 XZ2(this Vector3 v) => new (v.x, v.z);
  public static Vector3 XZ(this Vector3 v) => new (v.x, 0, v.z);
  public static void SetXZ(this ref Vector3 v, Vector3 a) {
    v.x = a.x;
    v.z = a.z;
  }
  public static Vector3 XZ(this Vector2 v) => new(v.x, 0, v.y);

  public static Vector3? TryGetDirection(this Vector3 origin, Vector3 target) {
    var direction = (target-origin).normalized;
    return direction.magnitude > 0 ? direction : null;
  }

  public static bool TryGetDirection(this Vector3 origin, Vector3 target, out Vector3 direction) {
    direction = (target-origin).normalized;
    return direction.sqrMagnitude > 0;
  }

  public static Vector3? TryGetDirection(this Vector3 vector) {
    return vector.sqrMagnitude > 0 ? vector.normalized : null;
  }

  public static float SqrDistance(this Vector3 a, Vector3 b) {
    return (a-b).sqrMagnitude;
  }
}

public static class NullableExtensions {
  public static T OrDefault<T>(this Nullable<T> nullable, T defaultValue) where T : struct {
    return nullable.HasValue ? nullable.Value : defaultValue;
  }
}

public static class NumberExtensions {
  public static float Sqr(this float f) => f*f;
  public static int ScaleBy(this int i, float f) => (int)((float)i*f);
}

public static class FlagLikeExtensions {
  public static AbilityTag AddFlags(this ref AbilityTag tag, AbilityTag mask) => tag |= mask;
  public static AbilityTag ClearFlags(this ref AbilityTag tag, AbilityTag mask) => tag &= ~mask;
  public static bool HasAllFlags(this AbilityTag tag, AbilityTag mask) => (tag & mask) == mask;
  public static bool HasAnyFlags(this AbilityTag tag, AbilityTag mask) => (tag & mask) != 0;
}

public static class GameObjectExtensions {
  public static void Destroy(this GameObject go) {
    if (go) {
      GameObject.Destroy(go);
    }
  }
}

public static class MonoExtensions {
  // Returns the (first matching) requested component on this object, creating it first if necessary.
  public static T GetOrCreateComponent<T>(this MonoBehaviour self) where T : Component {
    if (self.TryGetComponent(out T t)) {
      return t;
    } else {
      return self.gameObject.AddComponent<T>();
    }
  }
  public static void InitComponent<T>(this MonoBehaviour self, out T component, bool optional = false) where T : Component {
    component = self.GetComponent<T>();
    if (!optional)
      Debug.Assert(component, $"{self} is missing required component {typeof(T).Name}");
  }
  public static void InitComponentFromParent<T>(this MonoBehaviour self, out T component, bool optional = false) where T : Component {
    component = self.GetComponentInParent<T>();
    if (!optional)
      Debug.Assert(component, $"{self} is missing required component {typeof(T).Name}");
  }
  public static void InitComponentFromChildren<T>(this MonoBehaviour self, out T component, bool optional = false) where T : Component {
    component = self.GetComponentInChildren<T>();
    if (!optional)
      Debug.Assert(component, $"{self} is missing required component {typeof(T).Name}");
  }

  public static Transform FindDescendant(this Transform t, string name) {
    if (t.name == name)
      return t;

    for (var i = 0; i < t.childCount; i++) {
      var child = t.GetChild(i);
      if (child.name == name) {
        return child;
      } else {
        var fromSubtree = child.FindDescendant(name);
        if (fromSubtree) {
          return fromSubtree;
        }
      }
    }
    return null;
  }

  public static bool IsInFrontOf(this Vector3 p, Transform t) {
    var delta = p-t.position;
    var dot = Vector3.Dot(delta.normalized, t.forward);
    return dot >= 0;
  }

  public static bool IsInFrontOf(this Vector3 p, Transform t, out float dot) {
    var delta = p-t.position;
    dot = Vector3.Dot(delta.normalized, t.forward);
    return dot >= 0;
  }

  public static bool IsVisibleFrom(this Transform t, Vector3 p, LayerMask layerMask, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore) {
    var delta = t.position-p;
    var direction = delta.normalized;
    var distance = delta.magnitude;
    var didHit = Physics.Raycast(p, direction, out RaycastHit hit, distance, layerMask, triggerInteraction);
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

public static class AnimatorExtensions {
  public static void SetSpeed(this Animator a, float s) => a.speed = s;
}

public static class AnimationClipExtensions {
  public static float Milliseconds(this AnimationClip clip) => clip.length*1000;
}

public static class ArrayLikeExtensions {
  public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> enumerable) {
    var i = 0;
    foreach (var item in enumerable) {
      yield return (item, i++);
    }
  }

  public static T[] SortedCopy<T>(this T[] xs) {
    var ys = new T[xs.Length];
    xs.CopyTo(ys, 0);
    Array.Sort(ys);
    return ys;
  }

  public static void ForEach<T>(this IEnumerable<T> xs, Action<T> p) {
    foreach (var x in xs) {
      p(x);
    }
  }

  public static void ForEach<T>(this IEnumerable<T> xs, Action<T,int> p) {
    var index = 0;
    foreach (var x in xs) {
      p(x, index++);
    }
  }

  public static void Remove<T>(this List<T> xs, Predicate<T> predicate) {
    for (var i = xs.Count-1; i >= 0; i--) {
      if (predicate(xs[i])) {
        xs.RemoveAt(i);
      }
    }
  }

  public static void Remove<T>(this List<T> xs, Predicate<T> predicate, Action<T> action) {
    for (var i = xs.Count-1; i >= 0; i--) {
      if (predicate(xs[i])) {
        action(xs[i]);
        xs.RemoveAt(i);
      }
    }
  }

  public static Vector3 Sum<T>(this IEnumerable<T> xs, Func<T, Vector3> f) {
    var sum = Vector3.zero;
    foreach (var x in xs) {
      sum += f(x);
    }
    return sum;
  }

  public static T GetIndexOrDefault<T>(this T[] xs, int i, T def = default(T)) {
    if (i >= 0 && i < xs.Length) {
      return xs[i];
    } else {
      return def;
    }
  }
  public static bool TryGetIndex<T>(this T[] xs, int i, out T t) {
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

  public static void Shuffle<T>(this IList<T> list) {
    for (int n = list.Count-1; n > 0; n--) {
      int k = UnityEngine.Random.Range(0, n+1);
      (list[k], list[n]) = (list[n], list[k]);
    }
  }
}

public static class DictionaryExtensions {
  public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
    if (!dictionary.TryGetValue(key, out TValue value))
      dictionary.Add(key, value = default);
    return value;
  }
  public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createFunc) {
    if (!dictionary.TryGetValue(key, out TValue value))
      dictionary.Add(key, value = createFunc());
    return value;
  }
  // Special case for "counting" dictionaries, where a 0 indicates "key not present".
  public static int Increment<TKey>(this IDictionary<TKey, int> dictionary, TKey key, int amount = 1) {
    var current = dictionary.TryGetValue(key, out var value) ? value : 0;
    return dictionary[key] = current + amount;
  }
  public static int Decrement<TKey>(this IDictionary<TKey, int> dictionary, TKey key, int amount = 1) {
    var current = dictionary.TryGetValue(key, out var value) ? value : 0;
    var remaining = dictionary[key] = current - amount;
    if (remaining == 0)
      dictionary.Remove(key);
    return remaining;
  }
}

public static class CameraExtensions {
  public static Vector2 WorldToGUIPoint(this Camera cam, Vector3 worldPos) {
    var pos = cam.WorldToScreenPoint(worldPos);
    return new(pos.x, cam.pixelHeight - pos.y);
  }
}

public static class GUIExtensions {
  public static void DrawLine(Vector3 startWorld, Vector3 endWorld, int width) {
    var start = Camera.main.WorldToGUIPoint(startWorld);
    var end = Camera.main.WorldToGUIPoint(endWorld);
    Vector2 d = end - start;
    float a = Mathf.Rad2Deg * Mathf.Atan(d.y / d.x);
    if (d.x < 0)
      a += 180;

    int width2 = (int)Mathf.Ceil(width / 2f);

    GUIUtility.RotateAroundPivot(a, start);
    GUI.DrawTexture(new Rect(start.x, start.y - width2, d.magnitude, width), Texture2D.whiteTexture);
    GUIUtility.RotateAroundPivot(-a, start);
  }
  public static void DrawLabel(Vector3 worldPos, string label) => DrawLabel(worldPos, label, Color.white);
  public static void DrawLabel(Vector3 worldPos, string label, Color color) {
    var pos = Camera.main.WorldToGUIPoint(worldPos);
    var style = new GUIStyle() { fontSize = 32 };
    style.normal.textColor = color;
    GUI.Label(new Rect(pos.x, pos.y, 100, 100), label, style);
  }
}