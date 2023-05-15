using UnityEngine;

public class ElevatorSwitch : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] Elevator Elevator;

  void OnContact(MeleeContact contact) {
    Elevator.SetTarget.Fire(Target);
  }
}