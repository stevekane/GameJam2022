using UnityEngine;

public class ButtonEvents {
  public string Name;
  public EventSource JustDown;
  public EventSource Down;
  public EventSource JustUp;
  public ButtonEvents(string name) {
    Name = name;
    JustDown = new();
    Down = new();
    JustUp = new();
  }
}

public class InputManager : MonoBehaviour {
  public static InputManager Instance;

  public ButtonEvents R1 = new ButtonEvents("R1");
  public ButtonEvents R2 = new ButtonEvents("R2");
  public ButtonEvents L1 = new ButtonEvents("L1");
  public ButtonEvents L2 = new ButtonEvents("L2");

  void Awake() {
    Instance = this;
  }

  void Update() {
    BroadcastEvents(R1);
    BroadcastEvents(R2);
    BroadcastEvents(L1);
    BroadcastEvents(L2);
  }

  void BroadcastEvents(ButtonEvents events) {
    if (Input.GetButtonDown(events.Name)) {
      events.JustDown.Action?.Invoke();
    }
    if (Input.GetButton(events.Name)) {
      events.Down.Action?.Invoke();
    }
    if (Input.GetButtonUp(events.Name)) {
      events.JustUp.Action?.Invoke();
    }
  }
}