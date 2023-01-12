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
  void OnDrawGizmos() {
    Gizmos.DrawWireCube(
      new(.5f*(Left+Right), .5f*(Top+Bottom), .5f*(Front+Back)),
      new(Right-Left, Top-Bottom, Back-Front));
  }
}