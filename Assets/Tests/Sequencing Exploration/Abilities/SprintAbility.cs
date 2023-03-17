using System.Threading.Tasks;
using UnityEngine;

public class SprintAbility : MonoBehaviour {
  [SerializeField] float SprintMovementSpeed;
  [SerializeField] BaseMovementSpeed BaseMovementSpeed;
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] MaxMovementSpeed MaxMovementSpeed;
  [SerializeField] InputManager InputManager;

  public ButtonCode ButtonCode;

  public async Task Activate(TaskScope scope) {
    try {
      MovementSpeed.Value = SprintMovementSpeed;
      MaxMovementSpeed.Value = SprintMovementSpeed;
      await scope.ListenFor(InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp));
    } finally {
      InputManager.Consume(ButtonCode, ButtonPressType.JustUp);
      MovementSpeed.Value = BaseMovementSpeed.Value;
      MaxMovementSpeed.Value = BaseMovementSpeed.Value;
    }
  }
}