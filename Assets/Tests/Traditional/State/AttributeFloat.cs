using UnityEngine;

namespace Traditional {
  public class ModifierFloat {
    public float Add = 0;
    public float Mul = 1;
  }

  public abstract class AttributeFloat : MonoBehaviour {
    ModifierFloat Accumulator = new();
    ModifierFloat Current = new();
    public abstract float Base { get; set; }
    public float Value => (Base + Current.Add) * Current.Mul;
    public float Evaluate(float f) => (Base + f + Current.Add) * Current.Mul;
    public void Add(float v) => Accumulator.Add += v;
    public void Mul(float v) => Accumulator.Mul *= v;
    void FixedUpdate() {
      Current = Accumulator;
      Accumulator = new();
    }
  }
}