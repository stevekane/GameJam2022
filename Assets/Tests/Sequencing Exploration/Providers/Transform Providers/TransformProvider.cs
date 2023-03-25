using UnityEngine;

public abstract class TransformProvider : ScriptableObject {
  public abstract Transform Evaluate(GameObject gameObject);
}