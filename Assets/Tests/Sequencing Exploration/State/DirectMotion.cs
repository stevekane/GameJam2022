using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class DirectMotion : MonoBehaviour {
  int MotionPriority;
  Vector3 NextMotion;
  public Vector3 Motion { get; private set; }

  int ActivePriority;
  bool BaseActive = true;
  bool NextActive;
  public bool Active { get; private set; }

  [Header("Writes To")]
  [SerializeField] Velocity Velocity;

  public void AddMotion(Vector3 motion) {
    NextMotion += motion;
  }

  public void Override(Vector3 motion, int priority) {
    if (priority < MotionPriority || (priority == MotionPriority && motion.sqrMagnitude < NextMotion.sqrMagnitude))
      return;
    MotionPriority = priority;
    NextMotion = motion;
  }

  public void IsActive(bool isActive, int priority) {
    if (priority < ActivePriority || (priority == ActivePriority && !NextActive))
      return;
    ActivePriority = priority;
    NextActive = isActive;
  }

  void FixedUpdate() {
    if (NextActive) {
      Velocity.Value += NextMotion / Time.deltaTime;
    }
    Motion = NextMotion;
    NextMotion = Vector3.zero;
    MotionPriority = 0;
    Active = NextActive;
    NextActive = BaseActive;
    ActivePriority = 0;
  }
}