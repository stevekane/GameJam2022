using UnityEngine;

public class AblityMethodActionBinding : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] ButtonPressType ButtonPressType;
  [SerializeField] SampleAction Action;
  [SerializeField] string MethodName;

  AbilityMethodBinding AbilityMethodBinding;

  void Start() {
    AbilityMethodBinding = new() {
      AbilityManager = GetComponentInParent<AbilityManager>(),
      InputManager = GetComponentInParent<InputManager>(),
      Action = Action,
      Method = new() { MethodName = MethodName, Ability = GetComponent<Ability>() },
      ButtonCode = ButtonCode,
      ButtonPressType = ButtonPressType,
    };
    AbilityMethodBinding.ConsumedButtonEvents.Add(new(ButtonCode, ButtonPressType));
    AbilityMethodBinding.Bind();
  }

  void FixedUpdate() {
    AbilityMethodBinding.Update();
  }

  void OnDestroy() {
    AbilityMethodBinding.Unbind();
  }
}