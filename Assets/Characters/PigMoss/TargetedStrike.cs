using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TargetedStrike : MonoBehaviour {
  public GameObject SpawnPrefab;
  public float MinRadius = 0;
  public float MaxRadius = 1;
  public float MinAlpha = 0;
  public float MaxAlpha = 1;
  public AnimationCurve Radius = AnimationCurve.Constant(0,1,1);
  public AnimationCurve Alpha = AnimationCurve.Constant(0,1,1);
  public Timeval Duration = Timeval.FromSeconds(1);
  public DecalProjector Projector;

  IEnumerator Start() {
    const float MAX_PROJECTION_DEPTH = 10;
    var totalTicks = Duration.Ticks;
    for (var tick = 0; tick <= totalTicks; tick++) {
      var interpolant = (float)tick/(float)totalTicks;
      var radius = Mathf.Lerp(MinRadius, MaxRadius, Radius.Evaluate(interpolant));
      var alpha = Mathf.Lerp(MinAlpha, MaxAlpha, Alpha.Evaluate(interpolant));
      Projector.fadeFactor = alpha;
      Projector.size = new Vector3(radius, radius, MAX_PROJECTION_DEPTH);
      yield return null;
    }
    Destroy(Instantiate(SpawnPrefab, transform.position, transform.rotation), 5);
    Destroy(gameObject);
  }
}