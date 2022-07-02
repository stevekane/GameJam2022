using System;
using UnityEngine;

[Serializable]
public enum KnockBackType { 
  Delta, 
  Forward,
  Back,
  Right,
  Left,
  Up,
  Down
}

[CreateAssetMenu(fileName = "AttackConfig", menuName = "Attack/Config")]
public class AttackConfig : ScriptableObject {
  [Header("Frame data")]
  public Timeval Windup;
  public Timeval Active;
  public Timeval Contact;
  public Timeval Recovery;
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
  [Range(0,10)]
  public float WindupAnimationSpeed = 1;
  [Range(0,10)]
  public float ActiveAnimationSpeed = 1;
  [Range(0,10)]
  public float ContactAnimationSpeed = 0;
  [Range(0,10)]
  public float RecoveryAnimationSpeed = 1;
  [Range(0,10)]
  public float HitCameraShakeIntensity = 1;
  [Header("Damage")]
  public KnockBackType KnockBackType;
  [Range(0,100)]
  public float Points = 1;
  [Range(0,100)]
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