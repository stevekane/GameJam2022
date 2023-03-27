using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemTester : MonoBehaviour {
  [SerializeField] InputAction.CallbackContext Context;

  public void OnMove(InputAction.CallbackContext context) {
    Debug.Log(context);
    Debug.Log(1+1);
  }

  public void OnJump(InputAction.CallbackContext context) {
    Debug.Log("Jump");
  }
}