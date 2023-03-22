using System.Threading.Tasks;
using UnityEngine;

public enum RotationMatch {
  Target,
  LookAt
}

public class MotionWarping : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] InputManager InputManager;

  public int frame;
  public int total;
  public RotationMatch RotationMatch;
  public Transform target;
  public Vector3 targetOffset;

  TaskScope Scope;

  void Start() {
    InputManager.ButtonEvent(ButtonCode.L1, ButtonPressType.JustDown).Listen(PlayAnim);
    Scope = new();
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.L1, ButtonPressType.JustDown).Unlisten(PlayAnim);
    Scope.Dispose();
    Scope = null;
  }

  void PlayAnim() {
    InputManager.Consume(ButtonCode.L1, ButtonPressType.JustDown);
    Scope.Dispose();
    Scope = new();
    Scope.Start(Anim);
  }

  async Task Anim(TaskScope scope) {
    try {
      Animator.SetTrigger("Attack");
      for (var i = 0; i < 20; i++) {
        await scope.ListenFor(LogicalTimeline.FixedTick);
      }
      frame = 0;
      total = 20;
      for (var i = 0; i < 20; i++) {
        await scope.ListenFor(LogicalTimeline.FixedTick);
      }
    } finally {
      frame = total;
    }
  }

  Vector3 WarpMotion(Vector3 position, Vector3 target, Vector3 deltaPosition, int frame, int total) {
    var fraction = (float)frame/(float)total;
    var warpDelta = (target-position) / (total-frame);
    return Vector3.Lerp(deltaPosition, warpDelta, fraction);
  }

  Quaternion WarpRotation(Quaternion rotation, Quaternion target, Quaternion deltaRotation, int frame, int total) {
    var fraction = (float)frame/(float)total;
    var warpDelta = Quaternion.Slerp(Quaternion.identity, target * Quaternion.Inverse(rotation), fraction);
    var xyzRotation = Quaternion.Slerp(deltaRotation, warpDelta, fraction);
    var xyzEuler = xyzRotation.eulerAngles;
    return Quaternion.Euler(0, xyzEuler.y, 0);
  }

  void OnAnimatorMove() {
    if (frame < total) {
      if (RotationMatch == RotationMatch.Target) {
        var targetPosition = target.position + target.TransformVector(targetOffset);
        var targetRotation = target.rotation;
        transform.position += WarpMotion(transform.position, targetPosition, Animator.deltaPosition, frame, total);
        transform.rotation *= WarpRotation(transform.rotation, targetRotation, Animator.deltaRotation, frame, total);
      } else {
        var toTarget = (target.position - transform.position).normalized;
        var targetPosition = target.position - toTarget * targetOffset.magnitude;
        var targetRotation = toTarget.magnitude > 0 ? Quaternion.LookRotation(toTarget) : transform.rotation;
        transform.position += WarpMotion(transform.position, targetPosition, Animator.deltaPosition, frame, total);
        transform.rotation *= WarpRotation(transform.rotation, targetRotation, Animator.deltaRotation, frame, total);
      }
      frame++;
    } else {
      // want to ONLY take portion of rotation in the XZ-plane
      var rotation = Animator.deltaRotation * transform.rotation;
      var euler = rotation.eulerAngles;
      rotation = Quaternion.Euler(0, euler.y, 0);
      transform.rotation = rotation;
      transform.position += Animator.deltaPosition;
    }
  }
}