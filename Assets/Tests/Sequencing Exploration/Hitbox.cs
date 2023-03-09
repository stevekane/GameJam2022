using System;
using UnityEngine;

public enum HitDirection {
  Forward,
  Left,
  Right,
}

[Serializable]
public class HitboxConfig {
  public HitDirection HitDirection;
  public Timeval HitStopDuration = Timeval.FromAnimFrames(6, 60);
  public float KnockbackStrength = 0;
  public float CameraShakeIntensity = 1;
}

public class Hitbox : MonoBehaviour {
  public GameObject Owner;
  public Collider Collider;
  public HitDirection HitDirection;
  public Timeval HitStopDuration;
  public float KnockbackStrength;
  public float CameraShakeIntensity;
}