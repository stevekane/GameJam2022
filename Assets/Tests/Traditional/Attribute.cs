using UnityEngine;

namespace Traditional {
  public abstract class Attribute<T> : MonoBehaviour {
    public abstract T Base { get; set; }
    public abstract T Evaluate(T t);
  }
}