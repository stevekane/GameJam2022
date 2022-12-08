using System.Collections.Generic;
using UnityEngine;

public class HitParams {
  public GameObject VFX;
  public AudioClip SFX;
  public Vector3 KnockbackVector;
  public float KnockbackStrength;
  public float Damage;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;
  public List<StatusEffect> OnHitEffects;
  public Vector3? WallbounceTarget;
}