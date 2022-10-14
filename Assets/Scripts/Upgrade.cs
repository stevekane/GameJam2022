using UnityEngine;

public class Upgrade : ScriptableObject {
  public bool Permanent = true;
  public virtual void Activate(Upgrades us) => us.AddUpgrade(new() { Upgrade = this });
  public virtual void Load(Upgrades us, UpgradeData data) => us.AddUpgrade(data);
}
