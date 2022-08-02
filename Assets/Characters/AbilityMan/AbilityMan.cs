using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Status Status;
  AbilityFibered CurrentAbility;
  AimAndFireAbility AimAndFireAbility;
  GrappleAbilityFibered GrappleAbilityFibered;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    AimAndFireAbility = GetComponentInChildren<AimAndFireAbility>();
    GrappleAbilityFibered = GetComponentInChildren<GrappleAbilityFibered>();
    GrappleAbilityFibered.ButtonEvents = InputManager.Instance.L2;
    GrappleAbilityFibered.ButtonEvents.JustDown.Action += Grapple;
    InputManager.Instance.R2.JustDown.Action += TripleShot;
  }

  void OnDestroy() {
    InputManager.Instance.R2.JustDown.Action -= TripleShot;
    GrappleAbilityFibered.ButtonEvents.JustDown.Action -= Grapple;
  }

  void TryStartAbility(AbilityFibered ability) {
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
    TryStartAbility(GrappleAbilityFibered);
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