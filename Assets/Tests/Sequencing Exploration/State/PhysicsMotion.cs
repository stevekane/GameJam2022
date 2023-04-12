using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class PhysicsMotion : MonoBehaviour {
  [Header("Private Read Write")]
  int ActivePriority;
  bool BaseActive = true;
  bool NextActive;
  [field:SerializeField]
  public bool Active { get; private set; }

  int VelocityPriority;
  Vector3 NextVelocity;
  [field:SerializeField]
  public Vector3 PhysicsVelocity { get; private set; }

  [Header("Writes To")]
  [SerializeField] Velocity Velocity;

  public void AddVelocity(Vector3 velocity) {
    NextVelocity += velocity;
  }

  public void MultiplyVelocity(float scalar) {
    NextVelocity *= scalar;
  }

  public void OverrideVelocity(Vector3 velocity, int priority) {
    if (priority < VelocityPriority || priority == VelocityPriority && velocity.sqrMagnitude < NextVelocity.sqrMagnitude)
      return;
    VelocityPriority = priority;
    NextVelocity = velocity;
  }

  public void OverrideVelocityY(float y, int priority) {
    if (priority <= VelocityPriority)
      return;
    VelocityPriority = priority;
    NextVelocity.y = y;
  }

  public void IsActive(bool isActive, int priority) {
    if (priority < ActivePriority || (priority == ActivePriority && !NextActive))
      return;
    ActivePriority = priority;
    NextActive = isActive;
  }

  void FixedUpdate() {
    if (NextActive) {
      Velocity.Value += NextVelocity;
    }
    VelocityPriority = 0;
    PhysicsVelocity = NextVelocity;
    Active = NextActive;
    NextActive = BaseActive;
    ActivePriority = 0;
  }
}