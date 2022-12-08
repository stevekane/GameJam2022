using UnityEngine;

public class Damage : MonoBehaviour {
  Status Status;
  [SerializeField] float _Points;
  public float Points { get => _Points; private set => _Points = value; }

  void Awake() {
    Status = GetComponent<Status>();
  }

  void OnHit(HitParams hitParams) {
    AddPoints(hitParams.Damage);
    if (Status != null) {
      var power = 5f * hitParams.KnockbackStrength * Mathf.Pow((Points+100f) / 100f, 2f);
      var rotation = Quaternion.LookRotation(hitParams.KnockbackVector);
      Status.Add(new HitStopEffect(hitParams.KnockbackVector, .15f, hitParams.HitStopDuration.Ticks),
        s => s.Add(new KnockbackEffect(hitParams.KnockbackVector*power, hitParams.WallbounceTarget)));
      hitParams.OnHitEffects?.ForEach(e => Status.Add(e));
    }
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