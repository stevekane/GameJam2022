using System;
using UnityEngine;

public class Attack : MonoBehaviour {
  public AttackConfig Config;
  [HideInInspector] public Attacker Attacker;

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
}