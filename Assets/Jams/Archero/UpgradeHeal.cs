using System;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Heal")]
  public class UpgradeHeal : Upgrade {
    public float Fraction = .2f;
    public override void OnAdded(Upgrades us, int currentLevel) {
      var d = us.GetComponent<Damageable>();
      d.Heal(Mathf.RoundToInt(d.MaxHealth * Fraction));
      us.RemoveUpgrade(this);
    }
  }
}