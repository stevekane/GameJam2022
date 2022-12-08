using UnityEngine;

public class HitParams {
  public HitConfig HitConfig;
  public SerializedAttributes AttackerAttributes;
  public GameObject Attacker;
  public GameObject Source;
  public GameObject Defender;
  public SerializedAttributes DefenderAttributes;

  public HitParams(
  HitConfig hitConfig,
  SerializedAttributes attackerAttributes,
  GameObject attacker,
  GameObject source) {
    HitConfig = hitConfig;
    AttackerAttributes = attackerAttributes;
    Attacker = attacker;
    Source = source;
  }

  public void WithDefender(GameObject defender, SerializedAttributes defenderAttributes) {
    Defender = defender;
    DefenderAttributes = defenderAttributes;
  }
}