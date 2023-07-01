using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archero {
  public class Angel : MonoBehaviour {
    public List<Upgrade> Upgrades;
    public Upgrade Heal;
    TriggerEvent Hitbox;

    void OnTriggerEnter(Collider other) {
      if (other.TryGetComponent(out Player p)) {
        Hitbox.OnTriggerEnterSource.Unlisten(OnTriggerEnter);
        var us = p.GetComponent<Upgrades>();
        UpgradeUI.Instance.Show(us, "You've met an angel!", "Choose a blessing",
          us.PickUpgrades(Upgrades, 1).Append(Heal));
      }
    }

    void Awake() {
      this.InitComponentFromChildren(out Hitbox);
      Hitbox.OnTriggerEnterSource.Listen(OnTriggerEnter);
    }
  }
}