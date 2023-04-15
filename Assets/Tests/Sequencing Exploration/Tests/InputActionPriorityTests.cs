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

  void OnJump() {
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