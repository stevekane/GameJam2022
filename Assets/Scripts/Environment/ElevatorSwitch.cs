using UnityEngine;

public class ElevatorSwitch : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] Elevator Elevator;

  void OnHit(HitParams hitParams) {
    Elevator.SetTarget.Fire(Target);
  }
}