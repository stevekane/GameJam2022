using System.Collections.Generic;
using UnityEngine;

namespace Archero {
  public class MobManager : MonoBehaviour {
    public static MobManager Instance;

    public List<Mob> Mobs;

    void Awake() {
      Instance = this;
    }

    void OnDestroy() {
      Instance = null;
    }
  }
}