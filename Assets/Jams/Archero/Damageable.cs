using System;
using UnityEngine;
using UnityEngine.Events;

namespace Archero {
  [Serializable]
  public struct DamageEvent {
    public int Delta;
    public int Health;
    public int MaxHealth;
    public bool IsCrit;
    public bool IsHeadshot;
    public DamageEvent(int delta, int health, int maxHealth, bool isCrit = false, bool isHeadshot = false) {
      Delta = delta;
      Health = health;
      MaxHealth = maxHealth;
      IsCrit = isCrit;
      IsHeadshot = isHeadshot;
    }
  }

  public class Damageable : MonoBehaviour {
    [SerializeField] public int Health;
    [SerializeField] Attributes Attributes;
    [SerializeField] UnityEvent<DamageEvent> OnDamage;
    [SerializeField] UnityEvent OnDeath;
    GameObject LastAttacker;

    // Range [0,1].
    public int MaxHealth => (int)Attributes.GetValue(AttributeTag.Health, 0);
    public float HealthPct => Health / (float)MaxHealth;

    void Start() {
      BroadcastMessage("OnSpawn", new DamageEvent(0, Health, MaxHealth, false), SendMessageOptions.DontRequireReceiver);
    }

    void OnHurt(HitParams hitParams) {
      var headshot = hitParams.HeadshotRoll;
      var didCrit = hitParams.CritRoll;
      var damage = headshot ? Health : (int)hitParams.GetDamage(didCrit);
      LastAttacker = hitParams.Attacker;
      TakeDamage(damage, didCrit, headshot);
    }

    public void TakeDamage(int damage, bool didCrit = false, bool headshot = false) {
      if (damage == 0) return;
      Health = Mathf.Max(0, Health - damage);

      var damageEvent = new DamageEvent(-damage, Health, MaxHealth, didCrit, headshot);
      OnDamage.Invoke(damageEvent);
      BroadcastMessage("OnDamage", damageEvent, SendMessageOptions.DontRequireReceiver);

      if (Health <= 0) {
        OnDeath.Invoke();
        BroadcastMessage("OnDeath", SendMessageOptions.DontRequireReceiver);

        if (LastAttacker != null && LastAttacker.GetComponent<Attributes>().GetValue(AttributeTag.Bloodthirst, 0) > 0)
          LastAttacker.GetComponent<Damageable>().Heal((int)(.015 * MaxHealth));
        if (LastAttacker != null && LastAttacker.GetComponent<Attributes>().GetValue(AttributeTag.Inspire, 0) > 0)
          LastAttacker.GetComponent<Status>().Add(new InspireEffect());
        Destroy(gameObject);
      }
    }

    public void Heal(int amount) {
      Health = Mathf.Min(MaxHealth, Health + amount);
      var damageEvent = new DamageEvent(amount, Health, MaxHealth);
      OnDamage.Invoke(damageEvent);
      BroadcastMessage("OnDamage", damageEvent, SendMessageOptions.DontRequireReceiver);
    }
  }
}