using UnityEngine;

namespace Archero {
  public class SpikeEffect : StatusEffect {
    int Damage;
    int Ticks;
    int TicksRemaining;
    public Spikes Spikes { get; private set; }
    public SpikeEffect(Spikes spikes, float seconds, int damage) {
      Spikes = spikes;
      Damage = damage;
      Ticks = Timeval.FromSeconds(seconds).Ticks;
      TicksRemaining = Ticks;
    }
    public override bool Merge(StatusEffect e) => false;
    public override void Apply(Status status) {
      if (--TicksRemaining <= 0) {
        status.Damage.TakeDamage(Damage, false, false);
        TicksRemaining = Ticks;
      }
    }
  }

  public class Spikes : MonoBehaviour {
    [SerializeField] Team Team;
    [SerializeField] float DamagePeriod = 1;
    [SerializeField] int EnterDamage = 80;
    [SerializeField] int PeriodicDamage = 1;

    bool IsEffectFromThis(StatusEffect e) => (e as SpikeEffect)?.Spikes == this;

    void OnTriggerEnter(Collider c) {
      if (c.TryGetComponent(out Hurtbox hurtbox) &&
          hurtbox.Owner.TryGetComponent(out Team team) &&
          team.CanBeHurtBy(Team) &&
          hurtbox.Owner.TryGetComponent(out Status status)) {
        status.Damage.TakeDamage(EnterDamage, true, false);
        status.Add(new SpikeEffect(this, DamagePeriod, PeriodicDamage));
      }
    }

    void OnTriggerExit(Collider c) {
      if (c.TryGetComponent(out Hurtbox hurtbox) && hurtbox.Owner.TryGetComponent(out Status status)) {
        status.Remove(IsEffectFromThis);
      }
    }
  }
}