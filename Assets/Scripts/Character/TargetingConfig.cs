using UnityEngine;

[CreateAssetMenu(fileName = "TargetingConfig", menuName = "ActionConfiguration/TargetingConfig")]
public class TargetingConfig : ScriptableObject {
  public AnimationCurve DISTANCE_SCORE;
  public AnimationCurve ANGLE_SCORE;
  public int MAX_TARGETING_FRAMES = 300;
  [Range(0,100)] public float MAX_SEARCH_DISTANCE = 10f;
  [Range(0,180)] public float MAX_SEARCH_ANGLE = 90;
}