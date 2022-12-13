using UnityEngine;

/*
This is not a real class for gameplay.

This script drives a character's inputs directly and is meant
to be used to analyze how various animations look at runtime.
*/
public class BaseCharacterAutomaticInputDriver : MonoBehaviour {
  public enum MoveBehavior { Idle, RunInCircles }
  public enum AimBehavior { Idle, AimForward, AimAlong }

  [SerializeField] Vector2 AimDirection = Vector2.up;
  [SerializeField] MoveBehavior Move;
  [SerializeField] AimBehavior Aim;

  void Idle() {}
  void RunInCircles() {
    var time = Time.time;
    var x = Mathf.Cos(time*2);
    var z = Mathf.Sin(time*2);
    var v = new Vector3(x, 0, z);
    GetComponent<Mover>().SetMove(v);
  }
  void AimForward() {
    GetComponent<Mover>().SetAim(GetComponent<Mover>().GetMove());
  }
  void AimAlong(Vector2 v) {
    GetComponent<Mover>().SetAim(new Vector3(v.x, 0, v.y));
  }

  void FixedUpdate() {
    switch (Move) {
      case MoveBehavior.RunInCircles:
        RunInCircles();
      break;

      default:
        Idle();
      break;
    }
    switch (Aim) {
      case AimBehavior.AimForward:
        AimForward();
      break;

      case AimBehavior.AimAlong:
        AimAlong(AimDirection);
      break;

      default:
      break;
    }
  }

}