using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Status Status;
  Ability CurrentAbility;
  LightAttackAbility LightAttackAbility;
  AimAndFireAbility AimAndFireAbility;
  GrappleAbility GrappleAbility;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    LightAttackAbility = GetComponentInChildren<LightAttackAbility>();
    AimAndFireAbility = GetComponentInChildren<AimAndFireAbility>();
    GrappleAbility = GetComponentInChildren<GrappleAbility>();
    GrappleAbility.ButtonEvents = InputManager.Instance.L2;
    GrappleAbility.ButtonEvents.JustDown.Action += Grapple;
    InputManager.Instance.R2.JustDown.Action += TripleShot;
    InputManager.Instance.R1.JustDown.Action += LightAttack;
  }

  void OnDestroy() {
    InputManager.Instance.R1.JustDown.Action -= LightAttack;
    InputManager.Instance.R2.JustDown.Action -= TripleShot;
    GrappleAbility.ButtonEvents.JustDown.Action -= Grapple;
  }

  void TryStartAbility(Ability ability) {
    if (Status.CanAttack && CurrentAbility == null || !CurrentAbility.IsRunning) {
      ability.Stop();
      ability.Activate();
      CurrentAbility = ability;
    }
  }

  void TripleShot() {
    TryStartAbility(AimAndFireAbility);
  }

  void Grapple() {
    TryStartAbility(GrappleAbility);
  }

  void LightAttack() {
    TryStartAbility(LightAttackAbility);
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    if (CurrentAbility != null && !CurrentAbility.IsRunning) {
      CurrentAbility = null;
    }
    if (Status.CanRotate && move.magnitude > 0) {
      transform.forward = move;
    }
    if (Status.CanMove) {
      Controller.Move(dt*MOVE_SPEED*Inputs.Action.Left.XZ+dt*Physics.gravity);
    }
  }
}