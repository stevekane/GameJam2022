using System.Collections.Generic;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Group")]
  public class UpgradeGroup : Upgrade {
    public List<Upgrade> Upgrades;
    public override UpgradeData Add(Upgrades us) {
      foreach (var u in Upgrades)
        us.AddUpgrade(u);
      return new() { Upgrade = this, CurrentLevel = 1 };  // TODO: is this right? This is just a "shell" upgrade.
    }
  }
}