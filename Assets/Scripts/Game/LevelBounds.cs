using UnityEngine;

public class LevelBounds : MonoBehaviour {
  public float Bottom;
  public float Top;
  public float Left;
  public float Right;
  public float Front;
  public float Back;

  public bool IsInBounds(Vector3 pos) {
    return (
      pos.x > Left && pos.x < Right &&
      pos.y > Bottom && pos.y < Top &&
      pos.z > Front && pos.z < Back);
  }
  public Vector3 GetIntersectionNormal(Vector3 pos) {
    if (pos.x <= Left) return new(1, 0, 0);
    if (pos.x >= Right) return new(-1, 0, 0);
    if (pos.y <= Bottom) return new(0, 1, 0);
    if (pos.y >= Right) return new(0, -1, 0);
    if (pos.z <= Front) return new(0, 0, 1);
    if (pos.z >= Back) return new(0, 0, -1);
    Debug.LogError($"Trying to get LevelBounds intersection normal while inbounds: {pos}");
    return Vector3.zero;
  }
  void OnDrawGizmos() {
    Gizmos.DrawWireCube(
      new(.5f*(Left+Right), .5f*(Top+Bottom), .5f*(Front+Back)),
      new(Right-Left, Top-Bottom, Back-Front));
  }
}