using System.Collections.Generic;
using UnityEngine;

namespace Archero {
  public class Devil : MonoBehaviour {
    public List<Upgrade> Upgrades;
    public Upgrade Curse;
    TriggerEvent Hitbox;

    void OnTriggerEnter(Collider other) {
      if (other.TryGetComponent(out Player p)) {
        var amount = p.GetComponent<Attributes>().GetValue(AttributeTag.Health, 0) * .2f;
        var us = p.GetComponent<Upgrades>();
        UpgradeUI.Instance.Show(us, "You've met a devil!", $"Accept the devil's offer?\nLose {amount} Max HP",
          us.PickUpgrades(Upgrades, 1));
      }
    }

    void OnAccept((Upgrades us, Upgrade upgrade) args) {
      var amount = args.us.GetComponent<Attributes>().GetValue(AttributeTag.Health, 0) * .2f;
      WorldSpaceMessageManager.Instance.SpawnMessage($"-{amount} HP", args.us.transform.position + 2*Vector3.up, 2f);
      args.us.AddUpgrade(Curse);
    }

    void Awake() {
      this.InitComponentFromChildren(out Hitbox);
      Hitbox.OnTriggerEnterSource.Listen(OnTriggerEnter);
      UpgradeUI.Instance.OnChoose.Listen(OnAccept);
    }
    void OnDestroy() {
      UpgradeUI.Instance.OnChoose.Unlisten(OnAccept);
    }
  }
}