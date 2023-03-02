using System.Collections.Generic;
using UnityEngine;

public class SuckToTarget : MonoBehaviour {
  public float Height;
  public float MinDistance;
  public float MaxDistance;
  public float MaxSweepAngle;
  public float MaxVerticalAngle;
  public float IdealDistance;

  public List<GameObject> AllTargets;

  public int frames;
  public float distance;
  public List<float> RootDeltas;
  public List<float> Positions;

  public GameObject FindBestTarget() {
    return AllTargets[0];
  }

  void OnDrawGizmos() {
    var center = transform.position + Height * Vector3.up;
    var fov = MaxVerticalAngle;
    var aspectRatio = MaxSweepAngle / MaxVerticalAngle;
    Gizmos.color = Color.green;
    Gizmos.DrawFrustum(center, fov, MaxDistance, MinDistance, aspectRatio);

    // You have two possible moves on a frame: root and suck
    // You know how many frames you have to smoothly blend some combination of root and suck
    // As you have more frames, you weight root more highly
    // As you have fewer frames, you weight suck more highly
    // The result is you get an interpolated blend or a piece-wise crossfade between root and suck
    // This algorithm basically combines some fraction of root and suck motion on each frame
    var position = 0f;
    Positions.Clear();
    for (var i = 1; i <= frames; i++) {
      var root = RootDeltas[i-1];
      var suck = (distance-position)/(frames-(i-1)); // remaining distance over remaining frames
      var interpolant = (float)(i-1)/(float)(frames-1);
      var delta = Mathf.Lerp(root, suck, interpolant);
      position += delta;
      Positions.Add(position);
    }
    for (var i = 0; i < frames; i++) {
      Gizmos.color = Color.blue;
      Gizmos.DrawSphere(transform.position + 2 * transform.up + transform.forward*Positions[i], .1f);
    }
  }
}