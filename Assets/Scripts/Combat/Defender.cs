using UnityEngine;

[RequireComponent(typeof(Status), typeof(Damage))]
public class Defender : MonoBehaviour {
  Status Status;
  Damage Damage;
  bool PlayingFallSound;
  bool Died = false;
  public Vector3? LastGroundedPosition { get; private set; }
  public Hurtbox[] Hurtboxes;
  public EventSource<(HitParams, Transform)> HitEvent = new();
  public AudioClip FallSFX;

  void Awake() {
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    Hurtboxes.ForEach(hb => hb.gameObject.SetActive(Status.IsHittable));
    if (transform.position.y < -1f && !PlayingFallSound) {
      LastGroundedPosition = transform.position;
      PlayingFallSound = true;
      SFXManager.Instance.TryPlayOneShot(FallSFX);
    }
    if (transform.position.y < -100f) {
      Die();
    }
  }

  void OnHit(HitParams hitParams) {
    // STATUS STUFF
    var power = 5f * hitParams.KnockbackStrength * Mathf.Pow((Damage.Points+100f) / 100f, 2f);
    var rotation = Quaternion.LookRotation(hitParams.KnockbackVector);
    Status.Add(new HitStopEffect(hitParams.KnockbackVector, .15f, hitParams.HitStopDuration.Ticks),
      s => s.Add(new KnockbackEffect(hitParams.KnockbackVector*power, hitParams.WallbounceTarget)));
    hitParams.OnHitEffects?.ForEach(e => Status.Add(e));
    // DAMAGE STUFF
    Damage.AddPoints(hitParams.Damage);
  }

  public void Die() {
    if (Died)
      return;
    Died = true;
    // TODO: keep track of last attacker
    LastGroundedPosition = LastGroundedPosition ?? transform.position;
    SendMessage("OnDeath", SendMessageOptions.RequireReceiver);
  }
}