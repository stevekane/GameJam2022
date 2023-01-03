using UnityEngine;

namespace Traditional {
  public class ModifierVector3 {
    public Vector3 Add = Vector3.zero;
    public float Mul = 1;
  }

  public abstract class AttributeVector3 : MonoBehaviour {
    ModifierVector3 Accumulator = new();
    ModifierVector3 Current = new();
    public abstract Vector3 Base { get; set; }
    public Vector3 Value => (Base + Current.Add) * Current.Mul;
    public Vector3 Evaluate(Vector3 v) => (Base + v + Current.Add) * Current.Mul;
    public void Add(Vector3 v) => Accumulator.Add += v;
    public void Mul(float v) => Accumulator.Mul *= v;
    void FixedUpdate() {
      Current = Accumulator;
      Accumulator = new();
    }
  }
}