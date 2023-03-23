using UnityEngine;

public abstract class FloatProvider : ScriptableObject {
  public abstract float Evaluate(Animator animator);
}