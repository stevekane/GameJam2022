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
  // TODO: This doesn't belong here...
  [SerializeField] AnimationJobConfig FallenRecoverAnimation;
  [field:SerializeField] public float Points { get; private set; }

  Mover Mover;
  Status Status;
  Vibrator Vibrator;

  void Awake() {
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
    Vibrator = GetComponent<Vibrator>();
  }

  static readonly Timeval RecoilDuration = Timeval.FromAnimFrames(12, 60);
  static readonly float MinKnockbackForFallen = 20f;
  void OnHurt(HitParams hitParams) {
    AddPoints(hitParams.Damage);
    if (Status) {
      var knockbackVector = hitParams.KnockbackVector;
      var knockbackStrength = hitParams.GetKnockbackStrength(Points);
      var forward = hitParams.Source.transform.forward;
      if (Status.IsInterruptible) {
        Mover.ResetVelocityAndMovementEffects();
        Vibrator.VibrateOnHurt(forward, hitParams.GetHitStopDuration(Points).Ticks);
        Status.Add(new HitStopEffect(forward, hitParams.GetHitStopDuration(Points).Ticks),
          s => {
            if (knockbackStrength > MinKnockbackForFallen)
              s.Add(new FallenEffect(FallenRecoverAnimation));
            s.Add(new HurtStunEffect(hitParams.GetHurtStunDuration(Points).Ticks));
            s.Add(new SlowFallDuration(hitParams.HitConfig.SlowFallDuration.Ticks));
            s.Add(new KnockbackEffect(knockbackVector * knockbackStrength));
          });
      } else {
        Vibrator.VibrateOnHurt(forward, hitParams.GetHitStopDuration(Points).Ticks);
        Status.Add(new HitStopEffect(forward, hitParams.GetHitStopDuration(Points).Ticks));
      }
    }
  }

  void OnHit(HitParams hitParams) {
    var defenderDamage = hitParams.Defender.GetComponent<Damage>();
    var forward = hitParams.Source.transform.forward;
    Vibrator.VibrateOnHit(forward, hitParams.GetHitStopDuration(defenderDamage.Points).Ticks);
    Status.Add(new HitStopEffect(forward, hitParams.GetHitStopDuration(defenderDamage.Points).Ticks), s => {
      s.Add(new HitFollowthroughEffect(hitParams.HitConfig.RecoilStrength * forward, RecoilDuration, hitParams.Defender));
      //s.Add(new RecoilEffect(hitParams.HitConfig.RecoilStrength * forward));
    });
  }

  void OnWasParried(HitParams hitParams) {
    if (Status != null) {
      Status.Add(new RecoilEffect(hitParams.HitConfig.RecoilStrength * -hitParams.Source.transform.forward));
    }
  }

  void OnDidParry(HitParams hitParams) {
    // Hmm.. this is probably wrong. `hitParams` is the reflected attack. We probably want the parry attack's recoil here.
    Status.Add(new RecoilEffect(hitParams.HitConfig.RecoilStrength * -hitParams.Defender.transform.forward));
  }

  public void AddPoints(float dp) {
    if (Status == null || Status.IsDamageable)
      Points += dp;
  }
}