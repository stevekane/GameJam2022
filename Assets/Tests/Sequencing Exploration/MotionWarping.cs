using System.Threading.Tasks;
using UnityEngine;

public enum RotationMatch {
  Target,
  LookAt
}

public class MotionWarping : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] InputManager InputManager;

  public bool active;
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
      active = false;
      Animator.SetTrigger("Attack");
      for (var i = 0; i < 20; i++) {
        await scope.ListenFor(LogicalTimeline.FixedTick);
      }
    } finally {
      frame = 0;
      total = 20;
      active = true;
    }
  }

  // TODO: Do we constrain Y-axis motion? Currently...weird y-translation happens
  // TODO: Do we want this warp function? It seems there are potentially a lot of possible functions
  Vector3 WarpMotion(Vector3 position, Vector3 target, Vector3 deltaPosition, float fraction) {
    var warpDelta = (target - position) * fraction;
    return Vector3.Lerp(deltaPosition, warpDelta, fraction);
  }

  Quaternion WarpRotation(Quaternion rotation, Quaternion target, Quaternion deltaRotation, float fraction) {
    var warpDelta = Quaternion.Slerp(Quaternion.identity, target * Quaternion.Inverse(rotation), fraction);
    var xyzRotation = Quaternion.Slerp(deltaRotation, warpDelta, fraction);
    var xyzEuler = xyzRotation.eulerAngles;
    return Quaternion.Euler(0, xyzEuler.y, 0);
  }

  void OnAnimatorMove() {
    if (active) {
      var fraction = (float)frame/(float)total;
      if (RotationMatch == RotationMatch.Target) {
        var targetPosition = target.position + target.TransformVector(targetOffset);
        var targetRotation = target.rotation;
        transform.position += WarpMotion(transform.position, targetPosition, Animator.deltaPosition, fraction);
        transform.rotation *= WarpRotation(transform.rotation, targetRotation, Animator.deltaRotation, fraction);
      } else {
        var toTarget = (target.position - transform.position).normalized;
        var targetPosition = target.position - toTarget * targetOffset.magnitude;
        var targetRotation = toTarget.magnitude > 0 ? Quaternion.LookRotation(toTarget) : transform.rotation;
        transform.position += WarpMotion(transform.position, targetPosition, Animator.deltaPosition, fraction);
        transform.rotation *= WarpRotation(transform.rotation, targetRotation, Animator.deltaRotation, fraction);
      }
      active = frame <= total;
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