using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(ScriptExecutionGroups.Input)]
public class InputActionPriorityTests : MonoBehaviour {
  [Header("Inputs")]
  [SerializeField] InputActionAsset InputActions;

  [Header("Abilities")]
  [SerializeField] MockJumpAbility JumpAbility;
  [SerializeField] MockInteractAbility InteractAbility;
  [SerializeField] MockRemoteMissileAbility RemoteMissileAbility;
  [SerializeField] bool CanStartInteract;
  [SerializeField] EventSource OnCanStartInteract = new();
  [SerializeField] bool Stunned;
  [SerializeField] EventSource OnStunnedEvent = new();
  [SerializeField] bool Dead;
  [SerializeField] EventSource OnDeathEvent = new();

  InputAction JumpInputAction;
  InputAction InteractInputAction;
  InputAction ConfirmInputAction;
  InputAction CancelInputAction;
  InputAction FireInputAction;
  InputAction DetonateInputAction;

  [ContextMenu("Toggle Stun")]
  public void ToggleStun() {
    Stunned = !Stunned;
    if (Stunned)
      OnStunnedEvent.Fire();
  }

  [ContextMenu("Toggle Death")]
  public void ToggleDeath() {
    Dead = !Dead;
    if (Dead)
      OnDeathEvent.Fire();
  }

  [ContextMenu("Toggle Interact")]
  public void ToggleCanInteract() {
    CanStartInteract = !CanStartInteract;
    if (CanStartInteract)
      OnCanStartInteract.Fire();
  }

  void OnDeath() {
    InteractAbility.TryStop();
    RemoteMissileAbility.TryStop();
  }

  bool CanJump() =>
    JumpAbility.JumpAction.IsSatisfied() &&
    !Dead &&
    !Stunned &&
    !CanInteract() &&
    !InteractAbility.IsRunning;
  bool CanFire() =>
    RemoteMissileAbility.FireAction.IsSatisfied() &&
    !Dead &&
    !Stunned &&
    !InteractAbility.IsRunning;
  bool CanDetonate() =>
    RemoteMissileAbility.DetonateAction.IsSatisfied() &&
    !Dead &&
    !Stunned;
  bool CanInteract() =>
    InteractAbility.InteractAction.IsSatisfied() &&
    CanStartInteract &&
    !Dead &&
    !Stunned &&
    !RemoteMissileAbility.IsRunning;
  bool CanConfirm() =>
    InteractAbility.ConfirmAction.IsSatisfied() &&
    !Dead &&
    !Stunned;
  bool CanCancel() =>
    InteractAbility.CancelAction.IsSatisfied() &&
    !Dead &&
    !Stunned;

  /*
  Matt mentiond that there is a convention in the existing AbilityManager
  which allows an ability to be started when it would normally be blocked from starting
  if the blocking ability is cancelled by this new ability starting.

  First, a delineation of the of the key concepts:

    Can Take Action
    Condition prevents taking an Action
    Cancel existing action
    Check conditions for starting the new action
    If new conditions allow the action, then allow the action

  A way this could be done:

    Speculatively take the action
    Determine if the action is blocked
    If the action is blocked revert the action
    If the action is not blocked then allow it

  This is pretty tough to do in general.
  You would need universal undo on all actions in order to attempt a transaction
  and then be able to revert it if it is not allowed.

  Let's look at an example:

    CanMove: !Attacking
    OnMove: CancelAttack && Move

    OnMove()
    if (CanMove(State))
      Commit
    else
      Revert

  This would require that all the behaviors in OnMove were reversible or wrote to a provided
  state variable which could either be applied to the real state or discarded. This does not
  seem practical as it would constrain the entire system for a fairly niche use-case.

  Instead, handling conditions like these may require you to specifically model more conditions
  that would allow an ability to be started or not.

    Attacking blocks movement
    Movement cancels a cancellable Attack

    CanMove: !Attacking || AttackIsCancellable
    OnMove: CancelAttack && Move

  Here, you have created additional state in the model that explicitly allow the coordination
  desired. Specifically, AttackIsCancellable is now a part of the state and thus can be included
  in the predicate determining whether movement is allowed. Finally, cancelling that attack is
  an action taken when Move is performed.

  The result of this setup is that the following requirement is satisfied:

    Move when performing a cancellable attack should move and cancel that attack.
  */

  void Start() {
    JumpInputAction = InputActions.FindActionMap("Basics").FindAction("Jump");
    FireInputAction = InputActions.FindActionMap("RemoteMissile").FindAction("Fire");
    DetonateInputAction = InputActions.FindActionMap("RemoteMissile").FindAction("Detonate");
    InteractInputAction = InputActions.FindActionMap("Interactions").FindAction("Interact");
    ConfirmInputAction = InputActions.FindActionMap("Interactions").FindAction("Confirm");
    CancelInputAction = InputActions.FindActionMap("Interactions").FindAction("Cancel");
    OnDeathEvent.Action += OnDeath;
    JumpAbility.JumpAction.Action += OnJump;
  }

  // stuff that happens when you initiate a jump
  void OnJump() {
    // cancel some stuff
    // cancel running melee ability
  }

  void FixedUpdate() {
    JumpInputAction.SetEnabled(CanJump());
    FireInputAction.SetEnabled(CanFire());
    InteractInputAction.SetEnabled(CanInteract());
    DetonateInputAction.SetEnabled(CanDetonate());
    ConfirmInputAction.SetEnabled(CanConfirm());
    CancelInputAction.SetEnabled(CanCancel());
    if (JumpInputAction.WasPerformedThisFrame())
      JumpAbility.JumpAction.Fire();
    if (FireInputAction.WasPerformedThisFrame())
      RemoteMissileAbility.FireAction.Fire();
    if (InteractInputAction.WasPerformedThisFrame())
      InteractAbility.InteractAction.Fire();
    if (DetonateInputAction.WasPerformedThisFrame())
      RemoteMissileAbility.DetonateAction.Fire();
    if (ConfirmInputAction.WasPerformedThisFrame())
      InteractAbility.ConfirmAction.Fire();
    if (CancelInputAction.WasPerformedThisFrame())
      InteractAbility.CancelAction.Fire();
  }
}