using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttributeModifier {
  public float BonusMult { get; set; }
  public float BonusAdd { get; set; }
  public static float ApplyAll(float baseValue, List<AttributeModifier> ms) =>  (baseValue + ms.Sum(m => m.BonusAdd)) * ms.Aggregate(1f, (acc, m) => acc * m.BonusMult);
}

public enum AttributeTag {
  Damage,
  Health,
  MoveSpeed,
  AttackSpeed,
}

public class Upgrade {
  public bool Permanent = false;
  public virtual void OnAdded(UpgradeManager um) { }
  public virtual void OnRemoved(UpgradeManager um) { }
}

public class UpgradeAttribute : Upgrade {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
  public override void OnAdded(UpgradeManager um) {
    um.AddAttributeModifier(Attribute, Modifier);
  }
  public override void OnRemoved(UpgradeManager um) {
    um.RemoveAttributeModifier(Attribute, Modifier);
  }
}

public class UpgradeManager : MonoBehaviour {
  List<Upgrade> Upgrades;
  Dictionary<AttributeTag, List<AttributeModifier>> Modifiers = new();
  public float GetValue(float baseValue, AttributeTag attrib) {
    var list = Modifiers.GetValueOrDefault(attrib, null);
    return list == null ? baseValue : AttributeModifier.ApplyAll(baseValue, list);
  }
  public void AddUpgrade(Upgrade upgrade) {
    Upgrades.Add(upgrade);
    upgrade.OnAdded(this);
  }
  public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) {
    var list = Modifiers.GetOrAdd(attrib, () => new());
    list.Add(modifier);
  }
  public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) {
    Modifiers[attrib].Remove(modifier);
  }
}
