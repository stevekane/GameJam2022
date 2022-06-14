using UnityEngine;

public class Conveyor : MonoBehaviour {
  [SerializeField]
  int FramesPerCycle = 1000;  
  [SerializeField]
  Path Path;

  Bucket[] Buckets;
  int FramesRemaining;

  void FixedUpdate() {
    if (FramesRemaining > 0) {
      FramesRemaining = FramesRemaining-1;
    } else {
      FramesRemaining = FramesPerCycle;
    }
    var interpolant = (float)FramesRemaining/(float)FramesPerCycle;
    foreach (var bucket in Buckets) {
      var distance = interpolant+bucket.Distance;
      bucket.transform.position = Path.ToWorldSpace(distance%1f);
    }
  }

  void OnDrawGizmos() {
    Buckets = GetComponentsInChildren<Bucket>(false);
    if (!Application.isPlaying) {
      foreach (var bucket in GetComponentsInChildren<Bucket>(false)) {
        var position = Path.ToWorldSpace(bucket.Distance);
        bucket.transform.position = position;
        Gizmos.DrawWireSphere(position,.25f);
      }
    }
  }
}