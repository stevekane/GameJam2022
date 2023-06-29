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

    // Range [0,1].
    public float HealthPct => Health / Attributes.GetValue(AttributeTag.Health, 0);

    void Start() {
      var maxHealth = (int)Attributes.GetValue(AttributeTag.Health, 0);
      BroadcastMessage("OnSpawn", new DamageEvent(0, Health, maxHealth, false), SendMessageOptions.DontRequireReceiver);
    }

    void OnHurt(HitParams hitParams) {
      var headshot = hitParams.HeadshotRoll;
      var didCrit = hitParams.CritRoll;
      var damage = headshot ? Health : (int)hitParams.GetDamage(didCrit);
      TakeDamage(damage, didCrit, headshot);
    }

    public void TakeDamage(float damage, bool didCrit = false, bool headshot = false) {
      Health = Mathf.Max(0, Health - (int)damage);

      var maxHealth = (int)Attributes.GetValue(AttributeTag.Health, 0);
      var damageEvent = new DamageEvent((int)damage, Health, maxHealth, didCrit, headshot);
      OnDamage.Invoke(damageEvent);
      BroadcastMessage("OnDamage", damageEvent, SendMessageOptions.DontRequireReceiver);

      if (Health <= 0) {
        OnDeath.Invoke();
        BroadcastMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
      }
    }
  }
}