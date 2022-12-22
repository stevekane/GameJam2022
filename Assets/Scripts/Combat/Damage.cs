using System;
using UnityEngine;

[Serializable]
public enum KnockbackType {
  Delta,
  Forward,
}

public static class KnockbackTypeExtensions {
  /*
  KnockbackVector determined from choosing an attack axis and then a vector relative
  to that attack axis.

  For example, if you declare Attacker then <0,1,0> the resulting vector will be straight up
  along the attacker's forward direction.

  If you want to encode an AOE knock-away attack, you might chooise Delta then <0,0,1>
  which will knock all targets away from the attacker along the floor (z is forward)
  */
  public static Vector3 KnockbackVector(
  this KnockbackType type,
  float pitchAngle,
  Transform attacker,
  Transform target) {
    var direction = type switch {
      KnockbackType.Delta => attacker.position.TryGetDirection(target.position) ?? attacker.forward.XZ(),
      KnockbackType.Forward => attacker.forward.XZ(),
      _ => attacker.forward.XZ(),
    };
    return Vector3.RotateTowards(direction, Vector3.up, pitchAngle * Mathf.Deg2Rad, 0f);
  }
}

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
      Status.Add(new HitStopEffect(knockbackVector, hitParams.HitConfig.HitStopDuration.Ticks),
        s => {
          s.Add(new HurtStunEffect(hitParams.HitConfig.StunDuration.Ticks));
          s.Add(new SlowFallDuration(hitParams.HitConfig.SlowFallDuration.Ticks));
          s.Add(new KnockbackEffect(knockbackVector * knockbackStrength, wallbounceTarget));
      });
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