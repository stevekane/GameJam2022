using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fiber;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Status Status;
  Ability[] Abilities;
  Ability CurrentAbility;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Abilities = GetComponentsInChildren<Ability>();
    InputManager.Instance.R1.JustDown += LightAttack;
    InputManager.Instance.R2.JustDown += TripleShot;
    Fiber = new Fiber(PingPongForever());
  }

  void OnDestroy() {
    InputManager.Instance.R1.JustDown -= LightAttack;
    InputManager.Instance.R2.JustDown -= TripleShot;
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

  int FrameNumber = 0;
  Fiber Fiber;
  IEnumerator PingPongForever() {
    while (true) {
      var w1 = Random.Range(100,500);
      var w2 = Random.Range(100,500);
      var initial = FrameNumber;
      Debug.Log($"Waiting EITHER {w1} and {w2} frames");
      yield return Any(Wait(w1),Wait(w2));
      Debug.Log($"Waited {FrameNumber-initial} frames");
    }
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    Fiber.Run();
    FrameNumber++;

    if (CurrentAbility != null && CurrentAbility.IsComplete) {
      CurrentAbility = null;
    }
    if (Status.CanRotate && move.magnitude > 0) {
      transform.forward = move;
    }
    if (Status.CanMove) {
      Controller.Move(dt*MOVE_SPEED*Inputs.Action.Left.XZ);
    }
  }
}