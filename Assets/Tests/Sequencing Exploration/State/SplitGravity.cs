using UnityEngine;

public class SplitGravity : MonoBehaviour {
  [SerializeField] Velocity Velocity;
  public float FallGravity;
  public float RiseGravity;
  public float Value => Velocity.Value.y > 0 ? RiseGravity : FallGravity;
}