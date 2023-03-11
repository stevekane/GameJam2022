using UnityEngine;

public class MeleeAimAssist : MonoBehaviour {
  [SerializeField] CharacterController Controller;

  public int TotalTicks;
  public int Ticks;
  public float IdealDistance;

  void FixedUpdate() {
    if (TotalTicks > 0) {
      var target = FindObjectOfType<TargetDummyController>();
      var fraction = (float)Ticks / (float)TotalTicks;
      var toTarget = target.transform.position-transform.position;
      var idealPosition = target.transform.position-toTarget.normalized * IdealDistance;
      var toIdealPosition = idealPosition-transform.position;
      var toIdealPositionDelta = toIdealPosition * fraction;
      var desiredRotation = Quaternion.LookRotation(toTarget.normalized, transform.up);
      Controller.Move(toIdealPositionDelta);
      transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, fraction);
      Ticks++;
    }
  }
}