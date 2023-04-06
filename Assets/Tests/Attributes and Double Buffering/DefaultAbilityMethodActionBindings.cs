using UnityEngine;

public class DefaultAbilityMethodActionBindings : MonoBehaviour {
  [SerializeField] SampleAction MainAction;
  [SerializeField] SampleAction ReleaseAction;

  AbilityMethodBinding MainMethodBinding;
  AbilityMethodBinding ReleaseMethodBinding;

  void Start() {
    var ability = GetComponent<Ability>();
    var abilityManager = GetComponentInParent<AbilityManager>();
    var inputManager = GetComponentInParent<InputManager>();
    MainMethodBinding = new();
    MainMethodBinding.AbilityManager = abilityManager;
    MainMethodBinding.Action = MainAction;
    MainMethodBinding.Method = new() { MethodName = "MainAction", Ability = ability };
    ReleaseMethodBinding = new();
    ReleaseMethodBinding.AbilityManager = abilityManager;
    ReleaseMethodBinding.Action = ReleaseAction;
    ReleaseMethodBinding.Method = new() { MethodName = "MainRelease", Ability = ability };
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