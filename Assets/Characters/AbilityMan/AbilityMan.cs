using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;
  public CharacterController Controller;
  public Status Status;
  public SimpleAbility CurrentAbility;
  public SimpleAbility[] Abilities;

  bool TryStartAbility(SimpleAbility ability) {
    if (ability.IsComplete) {
      ability.Begin();
      CurrentAbility = ability;
      return true;
    } else {
      return false;
    }
  }

  void Start() {
    Abilities = GetComponentsInChildren<SimpleAbility>();
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    if (CurrentAbility != null && CurrentAbility.IsComplete) {
      CurrentAbility = null;
    }
    if (CurrentAbility == null) {
      if (action.R2.JustDown) {
        TryStartAbility(Abilities[0]);
      } else if (action.R1.JustDown) {
        TryStartAbility(Abilities[1]);
      }
    }

    if (CurrentAbility == null && move.magnitude > 0) {
      transform.forward = move;
    }
    if (Status.CanMove) {
      Controller.Move(dt*MOVE_SPEED*Inputs.Action.Left.XZ);
    }
  }
}