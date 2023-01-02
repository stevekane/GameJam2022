using UnityEngine;

namespace Traditional {
  public class ManualController : MonoBehaviour {
    [SerializeField] InputManager InputManager;
    [SerializeField] MoveDirection MoveDirection;
    [SerializeField] AimDirection AimDirection;

    void FixedUpdate() {
      MoveDirection.Base = InputManager.AxisLeft.XZ;
      AimDirection.Base = InputManager.AxisRight.XZ;
    }
  }
}