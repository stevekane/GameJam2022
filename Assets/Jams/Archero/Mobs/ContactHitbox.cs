using UnityEngine;

namespace Archero {
  public class ContactHitbox : MonoBehaviour {
    public enum Modes { Passive, Active };
    public HitConfig HitConfig;
    public Modes Mode = Modes.Passive;  // Active during dash attack, Passive does -90% mob damage.
    Attributes Owner;
    Team Team;

    void Awake() {
      Owner = Owner ?? transform.parent.gameObject.GetComponent<Attributes>();
      Team = Team ?? Owner.GetComponent<Team>();
    }

    void OnTriggerStay(Collider other) {
      if (other.gameObject.TryGetComponent(out Hurtbox hb)) {
        var hitConfig = Mode == Modes.Passive ? HitConfig.AddMult(-.9f) : HitConfig;
        hb.TryAttack(new(hitConfig, Owner.SerializedCopy, Owner.gameObject, Owner.gameObject));
      }
    }
  }
}