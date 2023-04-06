using UnityEngine;

public class RawAbilityMethodActionBinding : MonoBehaviour {
  [SerializeField] AbilityMethodBinding AbilityMethodBinding;

  void OnEnable() => AbilityMethodBinding.Bind();
  void OnDisable() => AbilityMethodBinding.Unbind();
  void FixedUpdate() => AbilityMethodBinding.Update();
}