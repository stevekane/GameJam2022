using UnityEngine;

public class ElevatorSwitch : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] Elevator Elevator;

  void OnHurt(HitParams hitParams) {
    Elevator.SetTarget.Fire(Target);
  }
}