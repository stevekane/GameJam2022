using System.Threading.Tasks;
using UnityEngine;

public class TripleFireAbility : MonoBehaviour {
  [SerializeField] InputSystemTester SystemTester;
  [SerializeField] InputManager InputManager;
  [SerializeField] GameObject BulletPrefab;
  public EventSource FireEventSource;

  public async Task Fire(TaskScope scope) {
    try {
      Debug.Log("FIRING START");
      // These consumes ensure nothing in the buffer carries over to this new state
      InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
      InputManager.Consume(ButtonCode.South, ButtonPressType.JustDown);
      // This is a very shitty way of blocking the input from some other system
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(Noop);
      await scope.Any(
        s => s.Ticks(60),
        s => s.Repeat(async delegate {
          await s.ListenFor(InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown));
          Fire();
        })
      );
    } finally {
      Debug.Log("FIRING COMPLETE");
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(Noop);
      SystemTester.StopAbility(this);
    }
  }

  void Fire() {
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    var bullet = Instantiate(BulletPrefab, transform.position, transform.rotation);
    bullet.gameObject.SetActive(true);
    bullet.GetComponent<Rigidbody>().AddForce(transform.forward * 10, ForceMode.Impulse);
  }

  void Noop() {}
}