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
      var w1 = Random.Range(100,500);
      var w2 = Random.Range(100,500);
      var initial = FrameNumber;
      Debug.Log($"Waiting EITHER {w1} and {w2} frames");
      yield return Either(WaitF(w1),WaitF(w2));
      Debug.Log($"Waited {FrameNumber-initial} frames");
    }
  }

  IEnumerator Either(IEnumerator a, IEnumerator b) {
    var astack = new Stack<IEnumerator>();
    var bstack = new Stack<IEnumerator>();
    astack.Push(a);
    bstack.Push(b);
    while (RunStack(astack) & RunStack(bstack)) {
      yield return null;
    }
  }

  IEnumerator Both(IEnumerator a, IEnumerator b) {
    var astack = new Stack<IEnumerator>();
    var bstack = new Stack<IEnumerator>();
    astack.Push(a);
    bstack.Push(b);
    while (RunStack(astack) | RunStack(bstack)) {
      yield return null;
    }
  }

  IEnumerator WaitF(int f) {
    for (var i = 0; i < f; i++) {
      yield return null;
    }
  }

  bool RunStack(Stack<IEnumerator> stack) {
    while (stack.TryPeek(out IEnumerator top)) {
      var running = top.MoveNext();
      if (!running) {
        stack.Pop();
      } else {
        if (top.Current is IEnumerator) {
          stack.Push(top.Current as IEnumerator);
        } else {
          return true;
        }
      }
    }
    return false;
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    RunStack(Stack);
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