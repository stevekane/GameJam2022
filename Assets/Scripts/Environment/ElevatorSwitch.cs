using UnityEngine;

public class ElevatorSwitch : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] Elevator Elevator;
  void OnDamage(DamageInfo info) {
    Elevator.SetTarget.Fire(Target);
  }
}