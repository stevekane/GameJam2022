using UnityEngine;

public class HurtState : MonoBehaviour {
  [SerializeField] LocalTime LocalTime;
  [SerializeField] SimpleAbilityManager AbilityManager;

  public int TicksRemaining;

  void FixedUpdate() {
    if (TicksRemaining == 0)
      AbilityManager.RemoveTag(AbilityTag.Hurt);
    else
      AbilityManager.AddTag(AbilityTag.Hurt);
    TicksRemaining = Mathf.Max(0, TicksRemaining-1);
  }
}