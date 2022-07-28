using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ButtonEvents {
  public string Name;
  public UnityAction JustDown;
  public UnityAction Down;
  public UnityAction JustUp;
  public ButtonEvents(string name) {
    Name = name;
  }
}

public class InputManager : MonoBehaviour {
  public static InputManager Instance;

  public ButtonEvents R1 = new ButtonEvents("R1");
  public ButtonEvents R2 = new ButtonEvents("R2");
  public ButtonEvents L1 = new ButtonEvents("L1");
  public ButtonEvents L2 = new ButtonEvents("L2");

  HashSet<GameObject> Subscribers = new();

  public void Subscribe(GameObject gameObject) {
    Subscribers.Add(gameObject);
  }

  public void Unsubscribe(GameObject gameObject) {
    Subscribers.Remove(gameObject);
  }

  void Awake() {
    Instance = this;
  }

  void Update() {
    foreach (var sub in Subscribers) {
      BroadcastMessages(R1.Name, sub);
      BroadcastMessages(R2.Name, sub);
      BroadcastMessages(L1.Name, sub);
      BroadcastMessages(L2.Name, sub);
    }
    BroadcastEvents(R1);
    BroadcastEvents(R2);
    BroadcastEvents(L1);
    BroadcastEvents(L2);
  }

  void BroadcastEvents(ButtonEvents events) {
    if (Input.GetButtonDown(events.Name)) {
      events.JustDown?.Invoke();
    }
    if (Input.GetButton(events.Name)) {
      events.Down?.Invoke();
    }
    if (Input.GetButtonUp(events.Name)) {
      events.JustUp?.Invoke();
    }
  }

  void BroadcastMessages(string name, GameObject sub) {
    if (Input.GetButtonDown(name)) {
      sub.SendMessage(name + "JustDown", SendMessageOptions.DontRequireReceiver);
    }
    if (Input.GetButton(name)) {
      sub.SendMessage(name + "Down", SendMessageOptions.DontRequireReceiver);
    }
    if (Input.GetButtonUp(name)) {
      sub.SendMessage(name + "JustUp", SendMessageOptions.DontRequireReceiver);
    }
  }
}