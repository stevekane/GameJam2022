using UnityEngine;

namespace Archero {
  public struct UpgradeDescription {
    public string Title;
  }

  public class Upgrade : ScriptableObject {
    public string DisplayName;
    public int MaxLevel = 18;
    // Called when the upgrade is newly added (level=1) or leveled up (level>1).
    public virtual void OnAdded(Upgrades us, int currentLevel) { }
    // Applies the effect of the current level of this upgrade.
    public virtual void Apply(Upgrades us) { }
    // Returns description data used for the UI.
    public virtual UpgradeDescription GetDescription(Upgrades us) => new() { Title = DisplayName };

    protected UpgradeData GetData(Upgrades us) => us.GetUpgradeData(this);
  }
}