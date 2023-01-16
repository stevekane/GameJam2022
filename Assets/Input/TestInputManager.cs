using System.Collections;
using UnityEngine;

/*
This example system listens for concurrent inputs and discriminates among them.
The simple thing it does it always chooses to attack if an attempt to attack
and an attempt to jump have happened recently but the attack is permitted
and the jump is permitted.
*/
public class TestInputManager : MonoBehaviour {
  enum PlayerState { Idle, Jumping, Attacking }

  [SerializeField] int FrameDelta = 10;
  [SerializeField] InputBuffer Buffer;
  [SerializeField] PlayerState State;

  void FixedUpdate() {
    var jumpPressed = Buffer.Pressed(ButtonCode.South, FrameDelta);
    var attackPressed = Buffer.Pressed(ButtonCode.West, FrameDelta);

    if (State == PlayerState.Idle) {
      if (jumpPressed.HasValue && attackPressed.HasValue) {
        StartCoroutine(Attack());
      } else if (jumpPressed.HasValue) {
        StartCoroutine(Jump());
      } else if (attackPressed.HasValue) {
        StartCoroutine(Attack());
      }
    }
  }

  IEnumerator Attack() {
    State = PlayerState.Attacking;
    Buffer.ConsumePress(ButtonCode.West);
    Debug.Log("Attack");
    yield return new WaitForSeconds(1);
    State = PlayerState.Idle;
  }

  IEnumerator Jump() {
    State = PlayerState.Jumping;
    Buffer.ConsumePress(ButtonCode.South);
    Debug.Log("Jump");
    yield return new WaitForSeconds(1);
    State = PlayerState.Idle;
  }
}