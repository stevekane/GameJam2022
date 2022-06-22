using UnityEngine;

[CreateAssetMenu(fileName = "Mob Config", menuName = "Mob/Config")]
public class MobConfig : ScriptableObject {
  public float MoveSpeed = 3f;
  public float BulletSpeed = 8f;
  public float ShootCooldown = 2f;
  public float TurnSpeedDeg = 20f;
  public float ShootRadius = 10f;
  public float SeekRadius = 20f;
  public AudioClip AttackAudioClip;
}
