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
    #region GROUND
    Inputs.Grounded.Run.SetEnabled(AbilityManager.CanRun(Locomotion.Move));
    Inputs.Grounded.Jump.SetEnabled(AbilityManager.CanRun(Jump.Jump));
    Inputs.Grounded.Slide.SetEnabled(AbilityManager.CanRun(Slide.Slide));
    Inputs.Grounded.Sprint.SetEnabled(AbilityManager.CanRun(Sprint.Sprint));
    Inputs.Grounded.LightAttack.SetEnabled(AbilityManager.CanRun(LightAttack.Attack));
    Inputs.Grounded.HeavyAttack.SetEnabled(AbilityManager.CanRun(HeavyAttack.Attack));
    #endregion

    #region AIR
    Inputs.Air.Jump.SetEnabled(AbilityManager.CanRun(DoubleJump.Jump));
    #endregion

    #region GROUND
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
      AbilityManager.Run(Slide.Slide);

    if (Inputs.Grounded.Sprint.IsInProgress())
      AbilityManager.Run(Sprint.Sprint);

    if (Inputs.Grounded.LightAttack.WasPerformedThisFrame())
      AbilityManager.Run(LightAttack.Attack);
    if (Inputs.Grounded.HeavyAttack.WasPerformedThisFrame())
      AbilityManager.Run(HeavyAttack.Attack);
    #endregion

    #region AIR
    if (Inputs.Air.Jump.WasPerformedThisFrame())
      AbilityManager.Run(DoubleJump.Jump);
    #endregion
  }
}