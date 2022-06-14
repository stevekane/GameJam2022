using UnityEngine;

public static class VectorExtensions {
  // Projects a Vector3 to the XZ plane. Used when we only care about horizontal movement.
  public static Vector3 XZ(this Vector3 v) {
    return new Vector3(v.x, 0, v.z);
  }
}