using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AbilityMethodBinding {
  public AbilityMethodReference Method;
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
  public InputManager InputManager { get; set; }
  public AbilityManager AbilityManager { get; set; }
  public SampleAction Action { get; set; }
  public bool AlwaysConsume;
  public void Bind() => InputManager.ButtonEvent(ButtonCode, ButtonPressType).Listen(Fire);
  public void Unbind() => InputManager.ButtonEvent(ButtonCode, ButtonPressType).Unlisten(Fire);
  public void Fire() {
    var fired = AbilityManager.TryInvoke(Method.GetMethod());
    if (fired || AlwaysConsume)
      InputManager.Consume(ButtonCode, ButtonPressType);
  }
}

public class ClassicAbilityBinding : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] List<AbilityMethodBinding> MethodBindings;

  AbilityMethodReference Main;
  AbilityMethodReference Release;
  SampleAction MainAction;
  SampleAction ReleaseAction;
  Ability Ability;
  AbilityManager AbilityManager;
  InputManager InputManager;

  void Awake() {
    MainAction = gameObject.AddComponent<SampleAction>();
    ReleaseAction = gameObject.AddComponent<SampleAction>();
    foreach (var binding in MethodBindings) {
      binding.Action = gameObject.AddComponent<SampleAction>();
    }
  }

  void Start() {
    Ability = GetComponent<Ability>();
    Main = new() { MethodName = "MainAction", Ability = Ability };
    Release = new() { MethodName = "MainRelease", Ability = Ability };
    AbilityManager = GetComponentInParent<AbilityManager>();
    InputManager = GetComponentInParent<InputManager>();
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustDown).Listen(TryMain);
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp).Listen(TryRelease);
    foreach (var binding in MethodBindings) {
      binding.InputManager = InputManager;
      binding.AbilityManager = AbilityManager;
      binding.Bind();
    }
  }

  void FixedUpdate() {
    MainAction.Satisfied = AbilityManager.CanInvoke(Main.GetMethod());
    ReleaseAction.Satisfied = AbilityManager.CanInvoke(Release.GetMethod());
    foreach (var binding in MethodBindings) {
      binding.Action.Satisfied = binding.AbilityManager.CanInvoke(binding.Method.GetMethod());
    }
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustDown).Unlisten(TryMain);
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp).Unlisten(TryRelease);
    if (MainAction)
      Destroy(MainAction);
    if (ReleaseAction)
      Destroy(ReleaseAction);
    foreach (var binding in MethodBindings) {
      binding.InputManager = InputManager;
      binding.AbilityManager = AbilityManager;
      binding.Unbind();
      Destroy(binding.Action);
    }
  }

  void TryMain() {
    if (AbilityManager.TryInvoke(Main.GetMethod())) {
      InputManager.Consume(ButtonCode, ButtonPressType.JustDown);
    }
  }

  void TryRelease() {
    AbilityManager.TryInvoke(Release.GetMethod());
    InputManager.Consume(ButtonCode, ButtonPressType.JustDown);
    InputManager.Consume(ButtonCode, ButtonPressType.JustUp);
  }
}