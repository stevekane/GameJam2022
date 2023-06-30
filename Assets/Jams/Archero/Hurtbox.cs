using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archero {
  public class Hurtbox : MonoBehaviour {
    const float BoltDist = 10f;

    public GameObject Owner;
    public Team Team;
    public float InvulnPeriod = 0f;

    int InvulnTicksRemaining = 0;

    void Awake() {
      Owner = Owner ?? transform.parent.gameObject;
      Team = Team ?? Owner.GetComponent<Team>();
    }

    public virtual bool CanBeHurtBy(HitParams hitParams) {
      if (!Team.CanBeHurtBy(hitParams.AttackerTeamID))
        return false;
      if (Owner.TryGetComponent(out Status status) && !status.IsHittable)
        return false;
      if (InvulnTicksRemaining > 0)
        return false;
      return true;
    }

    public virtual bool TryAttack(HitParams hitParams) {
      if (!CanBeHurtBy(hitParams)) return false;

      if (hitParams.AttackerAttributes.GetValue(AttributeTag.Bolt, 0) > 0) {
        var mobs = GetMobsWithin(BoltDist);
        foreach (var mob in mobs) {
          Bolt.Create(GameManager.Instance.BoltPrefab, Owner.transform, mob);
          var boltHit = hitParams.AddMult(-.75f);
          // Don't let Bolt trigger more bolts. This is pretty messy, should probably rethink how hitparams propagate.
          boltHit.AttackerAttributes.ClearModifier(AttributeTag.Bolt);
          mob.GetComponentInChildren<Hurtbox>().TryAttack(boltHit);
        }
      }

      InvulnTicksRemaining = Timeval.FromSeconds(InvulnPeriod).Ticks;
      hitParams.Defender = Owner;
      if (Owner.TryGetComponent(out Attributes defenderAttributes))
        hitParams.DefenderAttributes = defenderAttributes;
      hitParams.Defender?.SendMessage("OnHurt", hitParams, SendMessageOptions.DontRequireReceiver);
      hitParams.Source?.SendMessage("OnHit", hitParams, SendMessageOptions.DontRequireReceiver);

      return true;
    }

    Mob[] GetMobsWithin(float distance) {
      return MobManager.Instance.Mobs.Where(mob =>
        mob.gameObject != Owner &&
        (mob.transform.position - transform.position).sqrMagnitude < distance.Sqr()).ToArray();
    }

    void FixedUpdate() {
      if (InvulnTicksRemaining > 0)
        InvulnTicksRemaining--;
    }
  }
}