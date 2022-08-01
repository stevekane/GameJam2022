using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Status Status;
  Ability[] Abilities;
  Ability CurrentAbility;
  GrappleAbility GrappleAbility;
  GrappleAbilityFibered GrappleAbilityFibered;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Abilities = GetComponentsInChildren<Ability>();
    GrappleAbility = GetComponentInChildren<GrappleAbility>();
    GrappleAbilityFibered = GetComponentInChildren<GrappleAbilityFibered>();
    InputManager.Instance.R1.JustDown.Action += LightAttack;
    InputManager.Instance.R2.JustDown.Action += TripleShot;
    InputManager.Instance.L2.JustDown.Action += Grapple;
    InputManager.Instance.L1.JustDown.Action += Fibered;
  }

  void OnDestroy() {
    InputManager.Instance.R1.JustDown.Action -= LightAttack;
    InputManager.Instance.R2.JustDown.Action -= TripleShot;
    InputManager.Instance.L2.JustDown.Action -= Grapple;
    InputManager.Instance.L1.JustDown.Action -= Fibered;
  }

  void TripleShot() {
    if (Status.CanAttack && CurrentAbility == null || CurrentAbility.IsComplete) {
      Abilities[0].Begin();
      CurrentAbility = Abilities[0];
    }
  }

  void LightAttack() {
    if (Status.CanAttack && CurrentAbility == null || CurrentAbility.IsComplete) {
      Abilities[1].Begin();
      CurrentAbility = Abilities[1];
    }
  }

  void Grapple() {
    if (Status.CanAttack && CurrentAbility == null || CurrentAbility.IsComplete) {
      GrappleAbility.Stop();
      GrappleAbility.Activate();
    }
  }

  void Fibered() {
    GrappleAbilityFibered.Stop();
    GrappleAbilityFibered.Activate();
  }

  void LightAttackActive() {
    Debug.Log("Hi");
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    if (CurrentAbility != null && CurrentAbility.IsComplete) {
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