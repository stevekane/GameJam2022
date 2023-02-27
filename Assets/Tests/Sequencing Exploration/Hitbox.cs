using UnityEngine;

public class Hitbox : MonoBehaviour {
  public GameObject Owner;
  public Collider Collider;
  public HitDirection HitDirection;
  public Timeval KnockbackDuration;
  public float KnockbackStrength;
  public float CameraShakeIntensity;
}