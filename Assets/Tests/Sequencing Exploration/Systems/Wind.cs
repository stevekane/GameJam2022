using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class Wind : MonoBehaviour {
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] Vector3 Force;

  void FixedUpdate() {
    CharacterController.ApplyExternalForce(Force);
  }
}