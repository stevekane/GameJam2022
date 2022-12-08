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

public static class KnockBackTypeExtensions {
  public static Vector3 KnockbackVector(this KnockBackType type, Transform attacker, Transform target) {
    var p0 = attacker.position.XZ();
    var p1 = target.position.XZ();
    return type switch {
      KnockBackType.Delta => p0.TryGetDirection(p1) ?? attacker.forward,
      KnockBackType.Forward => attacker.forward,
      KnockBackType.Back => -attacker.forward,
      KnockBackType.Right => attacker.right,
      KnockBackType.Left => -attacker.right,
      KnockBackType.Up => attacker.up,
      KnockBackType.Down => -attacker.up,
      _ => attacker.forward,
    };
  }
}

[CreateAssetMenu(fileName = "AttackConfig", menuName = "Attack/Config")]
public class AttackConfig : ScriptableObject {
  [Header("Animation Clip")]
  public AnimationClip Clip;

  [Tooltip("Used to signal to animators what attack this is")]
  public int Index;

  [Header("Authoring frame data")]
  public Timeval Windup;
  public Timeval Active;
  public Timeval Recovery;

  [Header("Runtime frame data")]
  public Timeval WindupDurationRuntime;
  public Timeval ActiveDurationRuntime;
  public Timeval RecoveryDurationRuntime;
  public Timeval ContactDurationRuntime;

  public float WindupAnimationSpeed(bool IsCharging) {
    var scale = IsCharging ? 1f/ChargeDurationMultiplier : 1f;
    return scale*Windup.Millis/WindupDurationRuntime.Millis;
  }
  public float ActiveAnimationSpeed { get => Active.Millis/ActiveDurationRuntime.Millis; }
  public float RecoveryAnimationSpeed { get => Recovery.Millis/RecoveryDurationRuntime.Millis; }

  [Header("Movement")]
  [Range(0, 1)]
  public float WindupMoveFactor;
  [Range(0, 1)]
  public float ActiveMoveFactor;
  [Range(0, 1)]
  public float RecoveryMoveFactor;

  [Header("Rotation")]
  [Range(0, 360)]
  public float WindupRotationDegreesPerSecond;
  [Range(0, 360)]
  public float ActiveRotationDegreesPerSecond;
  [Range(0, 360)]
  public float RecoveryRotationDegreesPerSecond;

  [Header("Charging")]
  [Range(1, 10)]
  public int ChargeDurationMultiplier = 1;

  [Header("Camera")]
  [Range(0, 10)]
  public float HitCameraShakeIntensity = 1;

  [Header("Damage")]
  public KnockBackType KnockBackType;
  [Range(0, 100)]
  public float Points = 1;
  [Range(0, 100)]
  public float Strength = 1;

  [Header("Audio")]
  public AudioClip WindupAudioClip;
  public AudioClip ActiveAudioClip;
  public AudioClip RecoveryAudioClip;
  public AudioClip HitAudioClip;

  [Header("VFX")]
  public GameObject WindupEffect;
  public GameObject ActiveEffect;
  public GameObject ContactEffect;
  public GameObject RecoveryEffect;
}