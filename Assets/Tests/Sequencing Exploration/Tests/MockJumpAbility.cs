using UnityEngine;

public class MockJumpAbility : MonoBehaviour {
  public PotentialAction JumpAction;

  void Awake() {
    JumpAction = new(PotentialAction.True, Jump);
  }

  void Jump() {
    Debug.Log("Jump");
  }
}