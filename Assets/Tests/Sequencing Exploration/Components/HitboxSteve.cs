using System;
using System.Collections.Generic;
using UnityEngine;

public enum HitDirection {
  Forward,
  Left,
  Right,
}

[Serializable]
public struct HitboxParams {
  public HitDirection HitDirection;
  public int HitStopTicks; // Steve - int because value-type. plays better with timeline serialization
  public float KnockbackStrength;
  public float CameraShakeIntensity;
  public float Damage;
}

public class HitboxSteve : MonoBehaviour {
  public GameObject Owner;
  public Collider Collider;
  public HitboxParams HitboxParams;
  public List<GameObject> Targets = new();
}