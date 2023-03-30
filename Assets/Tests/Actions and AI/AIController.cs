using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Early)]
  public class AIController : MonoBehaviour {
    [SerializeField] GroundControl GroundControl;
    [SerializeField] MovementSpeed MovementSpeed;

    /*
    How does a player learn the affect of its abilities?
    They fire their actions and observe the affect on the game around them.
    They push a stick and observe that their character's orientation changes.
    They push a stick and observe that the camera moves.
    They push a stick and observe that their character moves.
    They push a button and observe that their character jumps.
    They push a button and observe that their character dashes a short distance.
    They release a button and observe that their character seems to fire a weapon in the direction they are facing.
    They observe that moving a stick while holding a button changes the direction they are facing.

    They realize that certain actions cause certain changes in the system.
    Changes in the system can, in-turn, cause additional changes in the system.
    Thus, they learn to predict what affects may occur in the world if they take an action.
    These can often be secondary or tertiary effects resulting from the direct change they take.
    Additionaly, the change they seek to enact on the world may happen in the future.

    Players have goals or objectives when playing the game and they issue actions they believe
    will help them achieve those goals based on their understanding of the actions direct and
    probable knock-on effects.
    */

    void FixedUpdate() {
      var target = FindObjectOfType<PlayerController>();
      if (GroundControl.CanStart()) {
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
        GroundControl.OnStart(input);
      }
    }
  }
}