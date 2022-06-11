using UnityEngine;

[CreateAssetMenu(fileName = "Move Config", menuName = "ActionConfiguration/MoveConfig")]
public class MoveConfig : ScriptableObject {
  public float MAX_ACCELERATION_MAGNITUDE = 10000f;
  public float MOVE_SPEED = 45f;
}