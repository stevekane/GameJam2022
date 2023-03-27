using System.Threading.Tasks;
using UnityEngine;

public class TripleFireAbility : MonoBehaviour {
  [SerializeField] InputSystemTester SystemTester;
  [SerializeField] InputManager InputManager;

  public async Task Fire(TaskScope scope) {
    try {
      Debug.Log("FIRING START");
      // These consumes ensure nothing in the buffer carries over to this new state
      InputManager.Consume(ButtonCode.South, ButtonPressType.JustDown);
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(Noop);
      InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
      InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(Fire);
      for (var i = 0; i < 60; i++) {
        await scope.ListenFor(FixedFrame.Instance.TickEvent);
      }
    } finally {
      Debug.Log("FIRING COMPLETE");
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(Noop);
      InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(Fire);
      SystemTester.StopAbility(this);
    }
  }

  void Fire() {
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    Debug.Log("Pew pew pew");
  }

  void Noop() {

  }
}