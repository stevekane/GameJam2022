using System.Collections.Generic;
using UnityEngine;

public class MobManager : MonoBehaviour {
  public static MobManager Instance;

  public List<Mob> Mobs;

  void Awake() => Instance = this;
}