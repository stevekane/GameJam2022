using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class Wind : MonoBehaviour {
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] Vector3 Force;

  void FixedUpdate() {
    PhysicsMotion.AddVelocity(Time.fixedDeltaTime * Force);
  }
}