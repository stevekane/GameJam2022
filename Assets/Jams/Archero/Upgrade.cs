using UnityEngine;

namespace Archero {
  public class Upgrade : ScriptableObject {
    // Returns the gold cost for the next level of this upgrade.
    public virtual int GetCost(Upgrades us) => 0;
    // Applies the effect of the current level of this upgrade.
    public virtual void Apply(Upgrades us) { }
    //// Returns description data used for the UI.
    //public virtual UpgradeDescription GetDescription(Upgrades us) => new() { Title = "Unknown" };
  }
}