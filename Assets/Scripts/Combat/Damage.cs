using UnityEngine;

public class Damage : MonoBehaviour {
  [field:SerializeField] public float Points { get; private set; }

  Mover Mover;
  Status Status;

  void Awake() {
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
  }

  void OnHurt(HitParams hitParams) {
    AddPoints(hitParams.Damage);
    if (Status != null) {
      var source = hitParams.Source.transform;
      var knockbackVector = hitParams.KnockbackVector;
      var knockbackStrength = 5f * hitParams.KnockbackStrength * Mathf.Pow((Points+100f) / 100f, 2f);
      var rotation = Quaternion.LookRotation(knockbackVector);
      var wallbounceTarget = Wallbounce.ComputeWallbounceTarget(source);
      Mover.ResetVelocity();
      Status.Add(new HurtStunEffect(hitParams.HitConfig.StunDuration.Ticks));
      Status.Add(new SlowFallDuration(hitParams.HitConfig.SlowFallDuration.Ticks));
      Status.Add(new HitStopEffect(knockbackVector, hitParams.HitConfig.HitStopDuration.Ticks),
        s => s.Add(new KnockbackEffect(knockbackVector * knockbackStrength, wallbounceTarget)));
    }
  }

  void OnHit(HitParams hitParams) {
    Status.Add(new HitStopEffect(hitParams.Source.transform.forward, hitParams.HitConfig.HitStopDuration.Ticks), s => {
      s.Add(new RecoilEffect(hitParams.HitConfig.RecoilStrength * -hitParams.Source.transform.forward));
    });
  }

  public void AddPoints(float dp) {
    if (Status) {
      if (Status.IsDamageable) {
        Points += dp;
      }
    } else {
      Points += dp;
    }
  }
}