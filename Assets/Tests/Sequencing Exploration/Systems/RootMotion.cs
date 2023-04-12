using UnityEngine;

// Steve - runs fixedupdate after MotionWarping
[DefaultExecutionOrder(ScriptExecutionGroups.Late+1)]
public class RootMotion : MonoBehaviour {
  int ActivePriority;
  bool BaseActive;
  bool NextActive;
  public bool Active { get; private set; }
  public Vector3 DeltaPosition { get; set; }
  public Quaternion DeltaRotation { get; set; }

  [Header("Reads From")]
  [SerializeField] Animator Animator;

  [Header("Writes To")]
  [SerializeField] Velocity Velocity;

  // TODO: Perhaps get rid of this notion of enabled
  // in favor of the new notion of double-buffered Active bools
  void Awake() {
    enabled = false;
  }

  // N.B. Fires during Animation Execution Group
  void OnAnimatorMove() {
    DeltaPosition = Animator.deltaPosition;
    DeltaRotation = Animator.deltaRotation;
  }

  void OnEnable() {
    BaseActive = true;
  }
  void OnDisable() {
    BaseActive = false;
  }

  // N.B. Fires during Late Execution Group
  void FixedUpdate() {
    if (NextActive) {
      Velocity.Value += DeltaPosition / Time.fixedDeltaTime;
      // TODO: Applying rotation directly here feels wrong
      // This should, like translation, probably be deferred
      // to some later system as many things could potentially
      // attempt to affect it.
      // However, these operations are likely commutative even
      // when distributed as rotation doesn't really interfere with
      // an environment like translation does.
      var rotation = DeltaRotation * transform.rotation;
      var euler = rotation.eulerAngles;
      rotation = Quaternion.Euler(0, euler.y, 0);
      transform.rotation = rotation;
    }
    ActivePriority = 0;
    NextActive = BaseActive;
  }
}