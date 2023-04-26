using UnityEngine;

public abstract class RootMotionProvider : ScriptableObject {
  public abstract bool Active(GameObject gameObject);
  public abstract Vector3 Position(GameObject gameObject);
  public abstract Quaternion Rotation(GameObject gameObject);
}