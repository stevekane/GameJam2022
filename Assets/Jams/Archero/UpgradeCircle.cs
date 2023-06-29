using System;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Circle")]
  public class UpgradeCircle : Upgrade {
    public SerializableEnum<AttributeTag> EffectType;
    public Material Material;
    public override void OnAdded(Upgrades us, int currentLevel) {
      Circle.Attach(GameManager.Instance.CirclePrefab, us.GetComponent<Attributes>(), EffectType, Material);
    }
  }
}