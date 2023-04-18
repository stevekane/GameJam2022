using UnityEngine;

public class AblityMethodActionBinding : MonoBehaviour {
  [SerializeField] SampleAction Action;
  [SerializeField] AbilityMethodReference Method;

  AbilityMethodBinding AbilityMethodBinding;

  void Start() {
    AbilityMethodBinding = new() {
      AbilityManager = GetComponentInParent<SimpleAbilityManager>(),
      Action = Action,
      Method = Method,
    };
    AbilityMethodBinding.Bind();
  }

  void FixedUpdate() {
    AbilityMethodBinding.Update();
  }

  void OnDestroy() {
    AbilityMethodBinding.Unbind();
  }
}