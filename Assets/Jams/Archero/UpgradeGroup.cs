using System.Collections.Generic;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Group")]
  public class UpgradeGroup : Upgrade {
    public List<Upgrade> Upgrades;
    public override void OnAdded(Upgrades us, int currentLevel) {
      foreach (var u in Upgrades)
        us.AddUpgrade(u);
    }
  }
}