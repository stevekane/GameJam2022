using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(ScriptExecutionGroups.Physics+1)]
public class GroundCheck : MonoBehaviour {
  const string ON_LAND_EVENT_NAME = "OnLand";
  const string ON_TAKEOFF_EVENT_NAME = "OnTakeoff";

  [Header("Reads From")]
  [SerializeField] LayerMask LayerMask;
  [SerializeField] CharacterController CharacterController;
  [SerializeField] float MaxGroundCheckDistance = 1f;
  [SerializeField] float GroundedDistanceThreshold = .2f;
  [Header("Writes To")]
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] Animator Animator;
  public bool IsGrounded { get; private set; }
  public float GroundDistance { get; private set; }

  RaycastHit Hit = new();

  void FixedUpdate() {
    var cylinderHeight = Mathf.Max(0, CharacterController.height - 2*CharacterController.radius);
    var offsetDistance = cylinderHeight / 2;
    var offset = offsetDistance*Vector3.up;
    var skinOffset = CharacterController.skinWidth*Vector3.up;
    var position = transform.TransformPoint(CharacterController.center + skinOffset - offset);
    var ray = new Ray(position, Vector3.down);
    var didHit = Physics.SphereCast(ray, CharacterController.radius, out Hit, MaxGroundCheckDistance, LayerMask);
    var isGrounded = didHit && Hit.distance <= GroundedDistanceThreshold;
    var wasGrounded = SimpleAbilityManager.Tags.HasFlag(AbilityTag.Grounded);

    if (isGrounded && !wasGrounded) {
      SendMessage(ON_LAND_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
    }
    if (!isGrounded && wasGrounded) {
      SendMessage(ON_TAKEOFF_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
    }
    if (isGrounded) {
      SimpleAbilityManager.AddTag(AbilityTag.CanJump);
      SimpleAbilityManager.AddTag(AbilityTag.Grounded);
    }
    IsGrounded = isGrounded;
    GroundDistance = didHit ? Hit.distance : float.MaxValue;
    Animator.SetBool("Grounded", IsGrounded);
    Animator.SetFloat("GroundDistance", GroundDistance);
  }

  void OnDrawGizmos() {
    #if UNITY_EDITOR
    Handles.Label(transform.position, Hit.collider ? $"Standing on: {Hit.collider.name}" : "");
    #endif
  }
}