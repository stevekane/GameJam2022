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
  public virtual void Add(Upgrades us) => us.AddUpgrade(new() { Upgrade = this });
  public virtual void Apply(Upgrades us) { }
  public virtual int GetCost(Upgrades us) => 0;
  public virtual UpgradeDescription GetDescription(Upgrades us) => new() { CurrentEffect = "Unknown" };
}
