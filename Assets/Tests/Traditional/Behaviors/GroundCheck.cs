using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Traditional {
  [DefaultExecutionOrder(-100)]
  public class GroundCheck : MonoBehaviour {
    [SerializeField] LayerMask LayerMask;
    [SerializeField] CharacterController CharacterController;
    [SerializeField] Grounded Grounded;
    [SerializeField] GroundDistance GroundDistance;
    [SerializeField] float MaxGroundCheckDistance = 1f;
    [SerializeField] float GroundedDistanceThreshold = .2f;
    RaycastHit Hit = new();

    void FixedUpdate() {
      var cylinderHeight = Mathf.Max(0, CharacterController.height - 2*CharacterController.radius);
      var offsetDistance = cylinderHeight / 2;
      var offset = offsetDistance*Vector3.up;
      var skinOffset = CharacterController.skinWidth*Vector3.up;
      var position = transform.TransformPoint(CharacterController.center + skinOffset - offset);
      var ray = new Ray(position, Vector3.down);
      var didHit = Physics.SphereCast(ray, CharacterController.radius, out Hit, MaxGroundCheckDistance, LayerMask);
      var grounded = didHit && GroundDistance.Value <= GroundedDistanceThreshold;
      var distance = didHit ? Hit.distance : float.MaxValue;
      if (Grounded.Value && !grounded) {
        SendMessage(Globals.TAKEOFF_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
      }
      if (!Grounded.Value && grounded) {
        SendMessage(Globals.LAND_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
      }
      Grounded.Value = grounded;
      GroundDistance.Value = distance;
    }

    void OnDrawGizmos() {
      #if UNITY_EDITOR
      Handles.Label(transform.position, Hit.collider ? $"Standing on: {Hit.collider.name}" : "");
      #endif
    }
  }
}