using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Aura")]
  public class UpgradeAura : Upgrade {
    public SerializableEnum<AttributeTag> Attribute;
    public AttributeModifier Modifier;
    public override void OnAdded(Upgrades us, int currentLevel) {
      GameManager.Instance.GlobalScope.Start(s => PeriodicEffect(s, us));
    }

    async Task PeriodicEffect(TaskScope scope, Upgrades us) {
      GameObject aura = null;
      try {
        while (true) {
          aura = Instantiate(GameManager.Instance.AuraPrefab, us.transform);
          var effect = us.GetComponent<Status>().Add(new InlineEffect(s => {
            s.AddAttributeModifier(Attribute, Modifier);
          }, "Aura"));
          await scope.Seconds(2f);
          us.GetComponent<Status>().Remove(effect);
          Destroy(aura);
          await scope.Seconds(8f);
        }
      } finally {
        Destroy(aura);
      }
    }
  }
}