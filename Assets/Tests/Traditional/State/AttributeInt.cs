using UnityEngine;

namespace Traditional {
  public class ModifierInt {
    public int Add = 0;
    public int Mul = 1;
  }

  public abstract class AttributeInt : MonoBehaviour {
    ModifierInt Accumulator = new();
    ModifierInt Current = new();
    public abstract int Base { get; set; }
    public int Value => (Base + Current.Add) * Current.Mul;
    public int Evaluate(int i) => (Base + i + Current.Add) * Current.Mul;
    public void Add(int v) => Accumulator.Add += v;
    public void Mul(int v) => Accumulator.Mul *= v;
    void FixedUpdate() {
      Current = Accumulator;
      Accumulator = new();
    }
  }
}