using UnityEngine;

public class Grabber : MonoBehaviour {
  public LineRenderer LineRenderer;

  Transform originTransform;
  Transform targetTransform;
  int flightFrame;
  int flightFrames;

  Vector3 PositionAt(Transform origin, Transform target, float fraction) {
    return fraction*(target.transform.position-origin.transform.position)+origin.transform.position;
  }

  public void Store(Vector3 position) {
    LineRenderer.positionCount = 1;
    LineRenderer.SetPosition(0, position);
  }

  public void Reach(Transform origin, Transform target, int frame, int frames) {
    originTransform = origin;
    targetTransform = target;
    flightFrame = frames-frame;
    flightFrames = frames;
  }

  void LateUpdate() {
    if (targetTransform && originTransform) {
      {
        var flightFraction = (float)flightFrame/(float)flightFrames;
        var nextPosition = PositionAt(originTransform, targetTransform, flightFraction);
        LineRenderer.positionCount = LineRenderer.positionCount+1;
        LineRenderer.SetPosition(LineRenderer.positionCount-1, nextPosition);
      }
      for (int i = 0; i < LineRenderer.positionCount; i++) {
        var flightFraction = (float)i/(float)flightFrames;
        var indexFraction = 1f-(float)i/(float)LineRenderer.positionCount;
        var desiredPosition = PositionAt(originTransform, targetTransform, flightFraction);
        var currentPosition = LineRenderer.GetPosition(i);
        var newPosition = Vector3.Lerp(currentPosition, desiredPosition, indexFraction);
        LineRenderer.SetPosition(i, newPosition);
      }
    }
    originTransform = null;
    targetTransform = null;
  }
}