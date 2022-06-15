using UnityEngine;

public struct PathData {
  public Vector3 Position;
  public Quaternion Rotation;
  public PathData(Vector3 position, Quaternion rotation) {
    Position = position;
    Rotation = rotation;
  }
}

public abstract class Path : MonoBehaviour {
  public abstract PathData ToWorldSpace(float interpolant);
}