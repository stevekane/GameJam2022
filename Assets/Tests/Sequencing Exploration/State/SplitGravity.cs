using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Early)]
public class SplitGravity : MonoBehaviour {
  [SerializeField] Velocity Velocity;
  [SerializeField] Gravity Gravity;
  public float FallGravity;
  public float RiseGravity;
  void FixedUpdate() => Gravity.Value = Velocity.Value.y < 0 ? FallGravity : RiseGravity;
}