using System;
using UnityEngine;

[Serializable]
public struct Effect {
  public GameObject GameObject;
  public float Duration;
}

[CreateAssetMenu(fileName = "SpawnConfig", menuName = "Encounters/SpawnConfig")]
public class SpawnConfig : ScriptableObject {
  public int Cost;
  public Effect PreviewEffect;
  public Effect SpawnEffect;
  public GameObject Mob;
}