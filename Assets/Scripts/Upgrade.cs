using UnityEngine;

public class Upgrade : ScriptableObject {
  public bool Permanent = true;
  public virtual void Activate(Upgrades us) => us.AddUpgrade(new() { Upgrade = this });
}
