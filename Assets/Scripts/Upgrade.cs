using UnityEngine;

public class Upgrade : ScriptableObject {
  public bool Permanent = true;
  public virtual void Add(Upgrades us) => us.AddUpgrade(new() { Upgrade = this });
  public virtual void Apply(Upgrades us) { }
  public virtual int GetCost(Upgrades us) => 0;
}
