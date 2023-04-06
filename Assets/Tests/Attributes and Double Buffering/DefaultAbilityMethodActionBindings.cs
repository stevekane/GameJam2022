using UnityEngine;

public class DefaultAbilityMethodActionBindings : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] SampleAction MainAction;
  [SerializeField] SampleAction ReleaseAction;

  AbilityMethodBinding MainMethodBinding;
  AbilityMethodBinding ReleaseMethodBinding;

  void Start() {
    var ability = GetComponent<Ability>();
    var abilityManager = GetComponentInParent<AbilityManager>();
    var inputManager = GetComponentInParent<InputManager>();
    var downEvent = new ButtonEvent(ButtonCode, ButtonPressType.JustDown);
    var upEvent = new ButtonEvent(ButtonCode, ButtonPressType.JustUp);
    MainMethodBinding = new();
    MainMethodBinding.AbilityManager = abilityManager;
    MainMethodBinding.InputManager = inputManager;
    MainMethodBinding.Action = MainAction;
    MainMethodBinding.Method = new() { MethodName = "MainAction", Ability = ability };
    MainMethodBinding.ButtonCode = ButtonCode;
    MainMethodBinding.ButtonPressType = ButtonPressType.JustDown;
    MainMethodBinding.ConsumedButtonEvents.Add(downEvent);
    MainMethodBinding.ConsumedButtonEvents.Add(upEvent);
    ReleaseMethodBinding = new();
    ReleaseMethodBinding.AbilityManager = abilityManager;
    ReleaseMethodBinding.InputManager = inputManager;
    ReleaseMethodBinding.Action = ReleaseAction;
    ReleaseMethodBinding.Method = new() { MethodName = "MainRelease", Ability = ability };
    ReleaseMethodBinding.ButtonCode = ButtonCode;
    ReleaseMethodBinding.ButtonPressType = ButtonPressType.JustUp;
    ReleaseMethodBinding.ConsumedButtonEvents.Add(downEvent);
    ReleaseMethodBinding.ConsumedButtonEvents.Add(upEvent);
    MainMethodBinding.Bind();
    ReleaseMethodBinding.Bind();
  }

  void FixedUpdate() {
    MainMethodBinding.Update();
    ReleaseMethodBinding.Update();
  }

  void OnDestroy() {
    MainMethodBinding.Unbind();
    ReleaseMethodBinding.Unbind();
  }
}