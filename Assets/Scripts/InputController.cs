using UnityEngine;

public class InputController : Controller {
  [SerializeField]
  float JoystickDeadzone = .1f;
  [SerializeField]
  float GrappleThreshold = .5f;

  bool GrappleReady = true;

  void Update() {
    var movex = Input.GetAxisRaw("MoveX");
    var movey = Input.GetAxisRaw("MoveY");
    var movevector = new Vector2(movex,movey);
    var aimx = Input.GetAxis("AimX");
    var aimy = Input.GetAxis("AimY");
    var aimvector = new Vector2(aimx,aimy);

    if (movevector.magnitude > JoystickDeadzone) {
      MoveX = movex;
      MoveY = movey;
    } else {
      MoveX = 0;
      MoveY = 0;
    }

    // This snarled shit is needed to generate only "new" Grapple inputs
    if (aimvector.magnitude > JoystickDeadzone) {
      if (aimvector.magnitude > GrappleThreshold) {
        if (GrappleReady) {
          Grapple = new Vector3(aimvector.x,0,aimvector.y).normalized;
          GrappleReady = false;
        } else {
          Grapple = null;
          GrappleReady = false;
        }
      } else {
        if (GrappleReady) {
          Grapple = null;
        } else {
          Grapple = null;
        }
      }
    } else {
      Grapple = null;
      GrappleReady = true;
    }
    
    Action1 = Input.GetButton("Action1");
    Action2 = Input.GetButton("Action2");
    Action3 = Input.GetButton("Action3");
    Action4 = Input.GetButton("Action4");

    // MP's secret hacky testing section
#if true
    if (Action1) {
      var knockable = GameObject.FindObjectOfType<Knockable>();
      knockable.Knock(new Vector3(aimx, 0, aimy));
    }
#endif
  }
}