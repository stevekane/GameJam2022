using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Upgrade {
  public bool Permanent = false;
  public virtual void OnAdded(Upgrades um) { }
  public virtual void OnRemoved(Upgrades um) { }
}

[Serializable]
public class UpgradeAttribute : Upgrade {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
  public override void OnAdded(Upgrades um) {
    um.AddAttributeModifier(Attribute, Modifier);
  }
  public override void OnRemoved(Upgrades um) {
    um.RemoveAttributeModifier(Attribute, Modifier);
  }
}

public class Upgrades : MonoBehaviour {
  List<Upgrade> Active = new();
  Dictionary<AttributeTag, AttributeModifier> Modifiers = new();
  public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
  public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
  public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);
  public void AddUpgrade(Upgrade upgrade) {
    Active.Add(upgrade);
    upgrade.OnAdded(this);
  }
}
