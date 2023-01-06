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
    RaycastHit Hit = new();

    void FixedUpdate() {
      const float GROUND_DISTANCE = .2f;
      var cylinderHeight = Mathf.Max(0, CharacterController.height - 2*CharacterController.radius);
      var offsetDistance = cylinderHeight / 2;
      var offset = offsetDistance*Vector3.up;
      var skinOffset = CharacterController.skinWidth*Vector3.up;
      var position = transform.TransformPoint(CharacterController.center + skinOffset - offset);
      var ray = new Ray(position, Vector3.down);
      var didHit = Physics.SphereCast(ray, CharacterController.radius, out Hit, GROUND_DISTANCE, LayerMask);
      if (Grounded.Value && !didHit) {
        SendMessage(Globals.TAKEOFF_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
      }
      if (!Grounded.Value && didHit) {
        SendMessage(Globals.LAND_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
      }
      Grounded.Value = didHit;
    }

    void OnDrawGizmos() {
      #if UNITY_EDITOR
      Handles.Label(transform.position, Hit.collider ? $"Standing on: {Hit.collider.name}" : "");
      #endif
    }
  }
}