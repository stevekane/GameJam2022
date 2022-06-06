using UnityEngine;

[CreateAssetMenu(fileName = "Ape Config", menuName = "Ape/Config")]
public class ApeConfig : ScriptableObject {
  public AnimationCurve MovementCurve;
  public AnimationCurve DistanceScore;
  public AnimationCurve AngleScore;
  public ActionConfig PounceConfig;
  public ActionConfig StompConfig;
  public int AimingFrames = 3000;
  public int FallFrames = 1500;
  public int JumpFrames = 250;
  public float GrabRadius = 1f;
  public float JumpDistance = 2f;
  public float SearchRadius = 100f;
  public float MoveSpeed = 10f;
  [Range(0,1)] public float AimThreshold = .2f;
  [Range(0,1)] public float MoveThreshold = .5f;
}