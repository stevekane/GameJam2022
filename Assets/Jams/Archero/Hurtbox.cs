using UnityEngine;

namespace Archero {
  public class Hurtbox : MonoBehaviour {
    public GameObject Owner;
    public Team Team;

    void Awake() {
      Owner = Owner ?? transform.parent.gameObject;
      Team = Team ?? Owner.GetComponent<Team>();
    }

    public virtual bool CanBeHurtBy(HitParams hitParams) {
      if (!Team.CanBeHurtBy(hitParams.AttackerTeamID))
        return false;
      if (Owner.TryGetComponent(out Status status) && !status.IsHittable)
        return false;
      return true;
    }

    public virtual bool TryAttack(HitParams hitParams) {
      if (!CanBeHurtBy(hitParams)) return false;

      hitParams.Defender = Owner;
      if (Owner.TryGetComponent(out Attributes defenderAttributes))
        hitParams.DefenderAttributes = defenderAttributes;
      hitParams.Defender?.SendMessage("OnHurt", hitParams, SendMessageOptions.DontRequireReceiver);
      hitParams.Source?.SendMessage("OnHit", hitParams, SendMessageOptions.DontRequireReceiver);
      return true;
    }
  }
}