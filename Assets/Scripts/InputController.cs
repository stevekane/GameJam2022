using UnityEngine;

public class InputController : Controller {
  public float JoystickDeadzone = .1f;
  public void Update() {
    var aimx = Input.GetAxis("AimX");
    var aimy = Input.GetAxis("AimY");
    var movex = Input.GetAxis("MoveX");
    var movey = Input.GetAxis("MoveY");
    AimX = Mathf.Abs(aimx) > JoystickDeadzone ? aimx : 0;
    AimY = Mathf.Abs(aimy) > JoystickDeadzone ? aimy : 0;
    MoveX = Mathf.Abs(movex) > JoystickDeadzone ? movex : 0;
    MoveY = Mathf.Abs(movey) > JoystickDeadzone ? movey: 0;
    Action1 = Input.GetButton("Action1");
    Action2 = Input.GetButton("Action2");
    Action3 = Input.GetButton("Action3");
    Action4 = Input.GetButton("Action4");
  }
}