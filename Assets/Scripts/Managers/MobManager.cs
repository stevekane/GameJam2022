using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobManager : MonoBehaviour {
  public static MobManager Instance;

  public List<Mob> Mobs;

  void Awake() {
    if (!Instance) {
      Instance = this;
    } else {
      Destroy(this);
    }
  }

  void OnDestroy() {
    if (Instance == this) {
      Instance = null;
    }
  }
}