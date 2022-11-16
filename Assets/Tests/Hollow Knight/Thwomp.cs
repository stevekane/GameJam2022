using System.Collections;
using UnityEngine;

public class Thwomp : MonoBehaviour {
  public AnimationCurve StartToEndCurve;
  public AnimationCurve EndToStartCurve;
  public Timeval StartDelay = Timeval.FromMillis(0);
  public Timeval StartDuration = Timeval.FromMillis(1000);
  public Timeval EndDuration = Timeval.FromMillis(1000);
  public Timeval StartToEndDuration = Timeval.FromMillis(1000);
  public Timeval EndToStartDuration = Timeval.FromMillis(1000);
  public Transform StartTransform;
  public Transform EndTransform;
  public Vector3 StartPosition;
  public Vector3 EndPosition;
  public AudioClip[] GroundImpactClips;
  public float ImpactShakeMagnitude;

  IEnumerator Routine;

  IEnumerator Tween(Transform t, Vector3 a, Vector3 b, Timeval duration, AnimationCurve curve) {
    for (var i = 0; i < duration.Ticks; i++) {
      t.position = Vector3.Lerp(a,b,curve.Evaluate((float)i/(float)duration.Ticks));
      yield return null;
    }
  }

  IEnumerator Cycle() {
    yield return Fiber.Wait(StartDelay.Ticks);
    while (true) {
      yield return Fiber.Wait(StartDuration.Ticks);
      yield return Tween(transform, StartPosition, EndPosition, StartToEndDuration, StartToEndCurve);
      CameraShaker.Instance.Shake(ImpactShakeMagnitude);
      AudioSource.PlayClipAtPoint(GroundImpactClips[UnityEngine.Random.Range(0,GroundImpactClips.Length)], transform.position);
      yield return Fiber.Wait(EndDuration.Ticks);
      yield return Tween(transform, EndPosition, StartPosition, EndToStartDuration, EndToStartCurve);
    }
  }

  void Start() {
    StartPosition = StartTransform.position;
    EndPosition = EndTransform.position;
    Routine = new Fiber(Cycle());
  }
  void OnDestroy() => Routine = null;
  void FixedUpdate() => Routine.MoveNext();

  void OnDrawGizmos() {
    Gizmos.DrawWireCube(StartTransform.position, Vector3.one);
    Gizmos.DrawWireCube(EndTransform.position, Vector3.one);
    Gizmos.DrawLine(StartTransform.position, EndTransform.position);
  }
}