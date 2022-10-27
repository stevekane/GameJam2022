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
  public virtual void Add(Upgrades us, bool purchase) => us.BuyUpgrade(new() { Upgrade = this }, 0, true);
  public virtual void Buy(Upgrades us) => Add(us, true);
  public virtual void Apply(Upgrades us) { }
  public virtual UpgradeDescription GetDescription(Upgrades us) => new() { Title = "Unknown" };
}
