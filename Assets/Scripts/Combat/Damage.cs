using UnityEngine;

public class Damage : MonoBehaviour {
  Status Status;
  [SerializeField] float _Points;
  public float Points { get => _Points; private set => _Points = value; }

  void Awake() {
    Status = GetComponent<Status>();
  }

  void OnHurt(HitParams hitParams) {
    Debug.LogWarning("Calculate damage and apply in Damage.OnHurt");
    // AddPoints(hitParams.Damage);
    if (Status != null) {
      var power = 5f * hitParams.HitConfig.KnockbackStrength * Mathf.Pow((Points+100f) / 100f, 2f);
      var attacker = hitParams.Attacker.transform;
      var defender = hitParams.Defender.transform;
      var knockbackType = hitParams.HitConfig.KnockbackType;
      var knockbackVector = knockbackType.KnockbackVector(attacker, defender);
      Debug.LogWarning("Calculate scaled knockbackstrength in Damage.OnHurt using SerializedAttributes");
      var knockbackStrength = hitParams.HitConfig.KnockbackStrength;
      var rotation = Quaternion.LookRotation(knockbackVector);
      var bounceboxTarget = Bouncebox.ComputeWallbounceTarget(attacker);
      Status.Add(new HitStopEffect(knockbackVector, .15f, hitParams.HitConfig.HitStopDuration.Ticks),
        s => s.Add(new KnockbackEffect(knockbackVector*knockbackStrength, bounceboxTarget)));
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