using UnityEngine;

public class ParryAllDamage : MonoBehaviour {
  public Animator Animator;

  void OnContact(GameObject other) {
    Animator.SetTrigger("Parry");
    Debug.Log($"You were hit by {other.name}");
  }
}