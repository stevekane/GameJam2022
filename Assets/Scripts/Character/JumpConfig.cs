using UnityEngine;

[CreateAssetMenu(fileName = "JumpConfig", menuName = "ActionConfiguration/JumpConfig")]
public class JumpConfig : ScriptableObject {
  public AnimationCurve STEERING_STRENGTH;
  public float MAX_STEERING_TIME = .5f;
  public float MAX_STEERING_FACTOR = 15;
  public float JUMP_Y_VELOCITY = 15f;
  public float JUMP_BOOST = 2f;
  public float JUMP_ANGLE = 30; // USED in current iteration of "Pounce" and "Dismount"
}