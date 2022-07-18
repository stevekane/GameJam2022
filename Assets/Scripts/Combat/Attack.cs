using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Attack : MonoBehaviour {
  public static Vector3 KnockbackVector(Transform attacker, Transform target, KnockBackType type) {
    var p0 = attacker.position.XZ();
    var p1 = target.position.XZ();
    return type switch {
      KnockBackType.Delta => p0.TryGetDirection(p1) ?? attacker.forward,
      KnockBackType.Forward => attacker.forward,
      KnockBackType.Back => -attacker.forward,
      KnockBackType.Right => attacker.right,
      KnockBackType.Left => -attacker.right,
      KnockBackType.Up => attacker.up,
      KnockBackType.Down => -attacker.up,
      _ => attacker.forward,
    };
  }
  
  public enum State { 
    Ready, 
    Windup, 
    Active, 
    Contact, 
    Recovery 
  }

  public enum Contact {
    Hit,
    Blocked,
    Parried
  }

  [Serializable]
  public class Outcome {
    public Contact Contact;
    public Hurtbox Hurtbox;
  }

  [Serializable]
  public class Phase {
    public Timeval AuthoringDuration = Timeval.FromMillis(3,30);
    public Timeval RuntimeDuration = Timeval.FromMillis(3,30);
    public float MoveSpeed = 1;
    public float TurnSpeed = 360;
    public float AnimationSpeed {
      get => AuthoringDuration.Millis/RuntimeDuration.Millis;
    }
  }

  public Attacker Attacker { get; set; }
  public AttackConfig Config;

  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox)) {
      Attacker?.Hit(hurtbox);
    }
  }
}