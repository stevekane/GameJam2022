using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public class TestSystem2 : MonoBehaviour {
  [SerializeField] BooleanAttribute CanMove;
  [SerializeField] BooleanAttribute CanRotate;
  [SerializeField] bool System2PreferredValue;

  void Update() {
    CanMove.Next.Vote(System2PreferredValue);
  }
}