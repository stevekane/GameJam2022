using UnityEngine;

public enum MobAIType {
  Wander,
}

[CreateAssetMenu(fileName = "Mob Config", menuName = "Mob/Config")]
public class MobConfig : ScriptableObject {
  public MobAIType AI = MobAIType.Wander;
  public float MoveSpeed = 10f;
}
