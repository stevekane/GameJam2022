using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Early)]
  public class AIController : MonoBehaviour {
    [SerializeField] ActionEventSourceVector3 GroundControlAction;
    [SerializeField] MovementSpeed MovementSpeed;

    void FixedUpdate() {
      var target = FindObjectOfType<InputManager>();
      if (GroundControlAction.IsAvailable) {
        var preferredPosition = target.transform.position + target.transform.forward * 10;
        var delta = (preferredPosition - transform.position).XZ();
        var direction = delta.normalized;
        var distance = delta.magnitude;
        var maxDistance = Time.deltaTime * MovementSpeed.Value;
        var input = distance switch {
          float d when d < .1f => Vector3.zero,
          float d when d < maxDistance => Mathf.InverseLerp(0, maxDistance, distance) * direction,
          _ => direction
        };
        GroundControlAction.Fire(input);
      }
    }
  }
}