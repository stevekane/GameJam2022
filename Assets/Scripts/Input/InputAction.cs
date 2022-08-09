using System.Collections.Generic;
using UnityEngine;

public enum InputActionTrigger {
  JustDown,
  Down,
  JustUp
}

[CreateAssetMenu(fileName = "InputAction", menuName = "Inputs/Action")]
public class InputAction : ScriptableObject, IEventSource {
  public string Name;
  public InputActionTrigger Trigger;
  public List<IEventSource> Connected { get; set; } = new();
  public void Fire() {
    Connected.ForEach(c => c.Fire());
  }
}