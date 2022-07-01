using UnityEngine;

public enum AttackWeight { Light, Heavy }

[CreateAssetMenu(fileName = "AttackConfig", menuName = "Attack/Config")]
public class AttackConfig : ScriptableObject {
  [Header("Frame data")]
  public Timeval Windup;
  public Timeval Active;
  public Timeval Contact;
  public Timeval Recovery;
  public Timeval Stun;
  [Header("Control")]
  [Range(0,1)]
  public float WindupMoveFactor;
  [Range(0,1)]
  public float ActiveMoveFactor;
  [Range(0,1)]
  public float ContactMoveFactor;
  [Range(0,1)]
  public float RecoveryMoveFactor;
  [Range(0,360)]
  public float WindupRotationDegreesPerSecond;
  [Range(0,360)]
  public float ActiveRotationDegreesPerSecond;
  [Range(0,360)]
  public float ContactRotationDegreesPerSecond;
  [Range(0,360)]
  public float RecoveryRotationDegreesPerSecond;
  [Header("Animation")]
  [Tooltip("Used to signal to animators what attack this is")]
  public int Index;
  public float WindupAnimationSpeed = 1;
  public float ActiveAnimationSpeed = 1;
  public float RecoveryAnimationSpeed = 1;
  public float HitCameraShakeIntensity = 1;
  [Header("Damage")]
  public float Points = 1;
  public float Strength = 1;
  [Header("Audio")]
  public AudioClip WindupAudioClip;
  public AudioClip ActiveAudioClip;
  public AudioClip RecoveryAudioClip;
  public AudioClip HitAudioClip;
  [Header("VFX")]
  public GameObject ActiveEffect;
  public GameObject HitEffect;
}