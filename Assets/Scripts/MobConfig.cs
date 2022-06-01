using UnityEngine;

public enum MobAIType {
  Stationary,
  Wander,
}

[CreateAssetMenu(fileName = "Mob Config", menuName = "Mob/Config")]
public class MobConfig : ScriptableObject {
  public MobAIType AI = MobAIType.Stationary;
  public float MoveSpeed = 10f;
}
