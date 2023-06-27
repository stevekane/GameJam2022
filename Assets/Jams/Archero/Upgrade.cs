using UnityEngine;

namespace Archero {
  public class Upgrade : ScriptableObject {
    public int MaxLevel = 18;
    // Adds the upgrade to the acquired or increases its level.
    public virtual UpgradeData Add(Upgrades us) => null;
    // Applies the effect of the current level of this upgrade.
    public virtual void Apply(Upgrades us) { }
    //// Returns description data used for the UI.
    //public virtual UpgradeDescription GetDescription(Upgrades us) => new() { Title = "Unknown" };
  }
}