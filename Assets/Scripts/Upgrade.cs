using UnityEngine;

public struct UpgradeDescription {
  public int CurrentLevel;
  public int Cost;
  public string Title;
  public string CurrentEffect;
  public string NextEffect;
  // TODO: cool icon
}

public class Upgrade : ScriptableObject {
  public bool Permanent = true;
  // Returns the gold cost for the next level of this upgrade.
  public virtual int GetCost(Upgrades us) => 0;
  // Returns the new UpgradeData to add to the Upgrades list, or null if nothing should be added (for example, if the existing UpgradeData was changed).
  public virtual UpgradeData Add(Upgrades us) => new() { Upgrade = this };
  // Applies the effect of the current level of this upgrade.
  public virtual void Apply(Upgrades us) { }
  // Returns description data used for the UI.
  public virtual UpgradeDescription GetDescription(Upgrades us) => new() { Title = "Unknown" };
}
