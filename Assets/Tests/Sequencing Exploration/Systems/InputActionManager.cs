using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Input)]
public class InputActionManager : MonoBehaviour {
  [SerializeField] PersonalCamera PersonalCamera;
  [SerializeField] SimpleAbilityManager AbilityManager;

  [Header("Grounded")]
  [SerializeField] LocomotionAbility Locomotion;
  [SerializeField] JumpAbility Jump;
  [SerializeField] SlideAblity Slide;
  [SerializeField] SprintAbility Sprint;
  [SerializeField] MeleeAttackAbility LightAttack;
  [SerializeField] MeleeAttackAbility HeavyAttack;
  [SerializeField] SimpleInteractAbility Interact;

  [Header("Airborne")]
  [SerializeField] DoubleJumpAbility DoubleJump;

  CharacterInputActions Inputs;

  void OnEnable() {
    Inputs?.Dispose();
    Inputs = new();
  }

  void OnDisable() {
    Inputs.Disable();
  }

  void OnDestroy() {
    Inputs?.Dispose();
  }

  void FixedUpdate() {
    Inputs.Grounded.Run.SetEnabled(AbilityManager.CanRun(Locomotion.Move));
    Inputs.Grounded.Jump.SetEnabled(AbilityManager.CanRun(Jump.Jump));
    Inputs.Grounded.Slide.SetEnabled(AbilityManager.CanRun(Slide.Main));
    Inputs.Grounded.Sprint.SetEnabled(AbilityManager.CanRun(Sprint.Sprint));
    Inputs.Grounded.LightAttack.SetEnabled(AbilityManager.CanRun(LightAttack.Main));
    Inputs.Grounded.HeavyAttack.SetEnabled(AbilityManager.CanRun(HeavyAttack.Main));
    Inputs.Air.Jump.SetEnabled(AbilityManager.CanRun(DoubleJump.Jump));
    Inputs.Interaction.Interact.SetEnabled(AbilityManager.CanRun(Interact.Main));
    Inputs.Interaction.Rotate.SetEnabled(AbilityManager.CanRun(Interact.Rotate));
    Inputs.Interaction.Confirm.SetEnabled(AbilityManager.CanRun(Interact.Confirm));
    Inputs.Interaction.Cancel.SetEnabled(AbilityManager.CanRun(Interact.Cancel));
    if (Inputs.Grounded.Run.enabled) {
      Locomotion.Value = PersonalCamera.CameraScreenToWorldXZ(Inputs.Grounded.Run.ReadValue<Vector2>());
      AbilityManager.Run(Locomotion.Move);
    } else {
      Locomotion.Value = Vector3.zero;
      AbilityManager.Run(Locomotion.Move);
    }
    if (Inputs.Grounded.Jump.WasPerformedThisFrame())
      AbilityManager.Run(Jump.Jump);
    if (Inputs.Grounded.Slide.WasPerformedThisFrame())
      AbilityManager.Run(Slide.Main);
    if (Inputs.Grounded.Sprint.IsInProgress())
      AbilityManager.Run(Sprint.Sprint);
    if (Inputs.Grounded.LightAttack.WasPerformedThisFrame())
      AbilityManager.Run(LightAttack.Main);
    if (Inputs.Grounded.HeavyAttack.WasPerformedThisFrame())
      AbilityManager.Run(HeavyAttack.Main);
    if (Inputs.Air.Jump.WasPerformedThisFrame())
      AbilityManager.Run(DoubleJump.Jump);
    if (Inputs.Interaction.Interact.WasPerformedThisFrame())
      AbilityManager.Run(Interact.Main);
    if (Inputs.Interaction.Rotate.WasPerformedThisFrame())
      AbilityManager.Run(Interact.Rotate);
    if (Inputs.Interaction.Confirm.WasPerformedThisFrame())
      AbilityManager.Run(Interact.Confirm);
    if (Inputs.Interaction.Cancel.WasPerformedThisFrame())
      AbilityManager.Run(Interact.Cancel);
  }
}