using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class MovementSpeed : MonoBehaviour {
  public float Base = 10;
  public float Max = 10;
  public float Value { get; private set; }

  float Sum = 0;
  float Mul = 1;

  public void Add(float a) {
    Sum += a;
  }

  public void Multiply(float m) {
    Mul += m;
  }

  void FixedUpdate() {
    Value = (Base+Sum) * Mul;
    Sum = 0;
    Mul = 1;
  }
}