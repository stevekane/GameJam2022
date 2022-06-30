using UnityEngine;

public enum AttackWeight { Light, Heavy }

[CreateAssetMenu(fileName = "AttackConfig", menuName = "Attack/Config")]
public class AttackConfig : ScriptableObject {
  [Tooltip("Used to signal to animators what attack this is")]
  public int Index;
  public Timeval Windup;
  public Timeval Active;
  public Timeval Contact;
  public Timeval Recovery;
  public Timeval Stun;
  public float WindupAnimationSpeed = 1;
  public float ActiveAnimationSpeed = 1;
  public float RecoveryAnimationSpeed = 1;
  public float HitCameraShakeIntensity = 1;
  public float Points = 1;
  public float Strength = 1;
  public AudioClip WindupAudioClip;
  public AudioClip ActiveAudioClip;
  public AudioClip RecoveryAudioClip;
  public AudioClip HitAudioClip;
  public GameObject ActiveEffect;
  public GameObject HitEffect;
}