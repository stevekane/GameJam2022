using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Status Status;
  SimpleAbility[] Abilities;
  SimpleAbility CurrentAbility;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Abilities = GetComponentsInChildren<SimpleAbility>();
    InputManager.Instance.R1.JustDown += LightAttack;
    InputManager.Instance.R2.JustDown += TripleShot;
    PingPong = PingPongForever();
    Stack = new();
    Stack.Push(PingPong);
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
  Stack<IEnumerator> Stack;
  IEnumerator PingPong;
  IEnumerator PingPongForever() {
    while (true) {
      var w1 = Random.Range(1000,5000);
      var w2 = Random.Range(1000,5000);
      var initial = FrameNumber;
      Debug.Log($"Waiting EITHER {w1} or {w2} frames");
      yield return Either(WaitF(w1),WaitF(w2));
      Debug.Log($"Waited {FrameNumber-initial} frames");
    }
  }

  IEnumerator Either(IEnumerator a, IEnumerator b) {
    while (a.MoveNext() & b.MoveNext()) {
      yield return null;
    }
  }

  IEnumerator Both(IEnumerator a, IEnumerator b) {
    while (a.MoveNext() | b.MoveNext()) {
      yield return null;
    }
  }

  IEnumerator WaitF(int f) {
    for (var i = 0; i < f; i++) {
      yield return null;
    }
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    // run top of the stack
    // if it yields an enumerable push it on the stack and run it
    // else if it not done then stop execution for this frame
    // if it is done then pop it from the stack
    if (null != PingPong && null != Stack) {
      while (Stack.Count > 0) {
        var top = Stack.Peek();
        var running = top.MoveNext();
        if (!running) {
          Stack.Pop();
        } else {
          if (top.Current is IEnumerator) {
            Stack.Push(top.Current as IEnumerator);
          } else {
            break;
          }
        }
      }
    }
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