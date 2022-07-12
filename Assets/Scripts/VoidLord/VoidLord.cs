using UnityEngine;

public abstract class VoidLordState : MonoBehaviour {
  public virtual void OnEnter(VoidLord voidLord) {}
  public virtual void OnExit(VoidLord voidLord) {}
  public abstract void Step(VoidLord voidLord, Action action, float dt);
}

public class VoidLord : MonoBehaviour {
  // Constants 
  public static float MOVE_SPEED = 10f;
  public static float DASH_SPEED = 50f;
  public static float TURN_SPEED = 720f;
  
  // Animator parameters
  public static int ACTION_INDEX = Animator.StringToHash("ActionIndex");
  public static int ATTACK_INDEX = Animator.StringToHash("AttackIndex");
  public static int IS_ATTACKING = Animator.StringToHash("Attacking");
  public static int ATTACK_SPEED = Animator.StringToHash("AttackSpeed");

  public static Quaternion RotationFromInputs(VoidLord voidLord, Action action, float dt) {
    var desiredForward = 
      action.Aim.XZ.TryGetDirection() ??
      action.Move.XZ.TryGetDirection() ?? 
      voidLord.transform.forward; 
    var currentRotation = voidLord.transform.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = dt*VoidLord.TURN_SPEED;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  public static Vector3 VelocityFromMove(Action action, float speed) {
    return speed*action.Move.XZ;
  }

  public void Push(Vector3 velocity) {
    Debug.Log("You pushed a VoidLord " + velocity);
  }

  public void Transition(VoidLordState from, VoidLordState to) {
    from.OnExit(this);
    State = to;
    to.OnEnter(this);
  }

  public CharacterController Controller;
  public Animator Animator;
  public Status Status;
  public Vector3 Velocity;
  public VoidLordState State;

  void FixedUpdate() {
    State.Step(this, Inputs.Action, Time.fixedDeltaTime);
  }
}