using UnityEngine;

namespace Traditional {
  public class Turning : MonoBehaviour {
    [SerializeField] AimDirection AimDirection;
    [SerializeField] TurnSpeed TurnSpeed;
    [SerializeField] LocalTimeScale LocalTimeScale;

    void FixedUpdate() {
      var dt = LocalTimeScale.Value * Time.fixedDeltaTime;
      var maxDegrees = dt * LocalTimeScale.Value * TurnSpeed.Value;
      var desiredForward = AimDirection.Value.sqrMagnitude switch {
        > 0 => AimDirection.Value,
        _   => transform.forward
      };
      var desiredRotation = Quaternion.LookRotation(desiredForward);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, maxDegrees);
    }
  }
}