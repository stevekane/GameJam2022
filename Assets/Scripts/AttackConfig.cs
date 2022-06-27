using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackConfig", menuName = "Attack Config")]
public class AttackConfig : ScriptableObject {
  public Timeval WindupTime;
  public Timeval ActiveTime;
  public Timeval RecoveryTime;
  public GameObject ActiveEffects;
}
