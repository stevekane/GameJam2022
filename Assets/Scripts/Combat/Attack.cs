using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Attack : MonoBehaviour {
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