using UnityEngine;

public interface ICondition {
  public bool Satisfied { get; }
}

public abstract class Condition : MonoBehaviour, ICondition {
  public abstract bool Satisfied { get; }
}