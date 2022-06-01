using UnityEngine;

[CreateAssetMenu(fileName = "Player Config", menuName = "Player/Config")]
public class PlayerConfig : ScriptableObject {
  public AnimationCurve MovementCurve;
  public float MoveSpeed = 10f;
  public float RollSpeed = 20f;
  public float RollDuration = .5f;
  public float MaxRollRadiansPerSecond = Mathf.PI / 2;
  public float RollVaultScaleFactor = 1.5f;
}