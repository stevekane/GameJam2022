using UnityEngine;

public class Hitbox : MonoBehaviour {
  public GameObject Owner;
  public Collider Collider;
  public HitDirection HitDirection;
  public Timeval HitStopDuration;
  public float KnockbackStrength;
  public float CameraShakeIntensity;
}