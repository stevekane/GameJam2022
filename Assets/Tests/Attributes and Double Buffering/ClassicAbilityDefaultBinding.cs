using UnityEngine;

public class ClassicAbilityDefaultBinding : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;

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
  }

  void Start() {
    Ability = GetComponent<Ability>();
    Main = new() { MethodName = "MainAction", Ability = Ability };
    Release = new() { MethodName = "MainRelease", Ability = Ability };
    AbilityManager = GetComponentInParent<AbilityManager>();
    InputManager = GetComponentInParent<InputManager>();
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustDown).Listen(TryMain);
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp).Listen(TryRelease);
  }

  void FixedUpdate() {
    MainAction.Satisfied = AbilityManager.CanInvoke(Main.GetMethod());
    ReleaseAction.Satisfied = AbilityManager.CanInvoke(Release.GetMethod());
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustDown).Unlisten(TryMain);
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp).Unlisten(TryRelease);
    if (MainAction)
      Destroy(MainAction);
    if (ReleaseAction)
      Destroy(ReleaseAction);
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