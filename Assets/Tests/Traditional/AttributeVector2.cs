using UnityEngine;

namespace Traditional {
  public class ModifierVector2 {
    public Vector2 Add = Vector2.zero;
    public float Mul = 1;
  }

  public abstract class AttributeVector2 : MonoBehaviour {
    ModifierVector2 Accumulator = new();
    ModifierVector2 Current = new();
    public abstract Vector2 Base { get; set; }
    public Vector2 Value => (Base + Current.Add) * Current.Mul;
    public Vector2 Evaluate(Vector2 v) => (Base + v + Current.Add) * Current.Mul;
    public void Add(Vector2 v) => Accumulator.Add += v;
    public void Mul(float v) => Accumulator.Mul *= v;
    void FixedUpdate() {
      Current = Accumulator;
      Accumulator = new();
    }
  }
}