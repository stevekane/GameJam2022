using System.Collections;
using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Status Status;
  Ability[] Abilities;
  Ability CurrentAbility;
  GrappleAbility GrappleAbility;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Abilities = GetComponentsInChildren<Ability>();
    GrappleAbility = GetComponentInChildren<GrappleAbility>();
    InputManager.Instance.R1.JustDown.Action += LightAttack;
    InputManager.Instance.R2.JustDown.Action += TripleShot;
    // InputManager.Instance.L2.JustDown += Grapple;
    StartCoroutine(Bar());
  }

  void OnDestroy() {
    InputManager.Instance.R1.JustDown.Action -= LightAttack;
    InputManager.Instance.R2.JustDown.Action -= TripleShot;
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

  IEnumerator Bar() {
    var charge = InputManager.Instance.L1.JustDown;
    var cancel = InputManager.Instance.L2.JustDown;
    var op1 = new Choose(charge, cancel);
    yield return StartCoroutine(op1);
    if (op1.Value) {
      Debug.Log("Charging...");
      var fire = InputManager.Instance.L1.JustUp;
      var op2 = new Switch(fire, cancel);
      yield return StartCoroutine(op2);
      switch (op2.Value) {
        case 0: {
          Debug.Log("Fire!");
        }
        break;
        default: {
          Debug.Log("Canceled");
        }
        break;
      }
    } else {
      Debug.Log("Canceled");
    }
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