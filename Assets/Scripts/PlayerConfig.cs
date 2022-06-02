using UnityEngine;

[CreateAssetMenu(fileName = "Player Config", menuName = "Player/Config")]
public class PlayerConfig : ScriptableObject {
  public AnimationCurve MovementCurve;
  public AnimationCurve DistanceScore;
  public AnimationCurve AngleScore;
  public float SearchRadius = 100f;
  public float MoveSpeed = 10f;
  public float RollSpeed = 20f;
  public float RollDuration = .5f;
  public float SpinSpeed = 20f;
  public float SpinDuration = 1f;
  public float MaxRollRadiansPerSecond = Mathf.PI / 2;
  public float MaxSpinRadiansPerSecond = Mathf.PI;
  public float RollVaultScaleFactor = 1.5f;
}