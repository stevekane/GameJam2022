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
    public DamageEvent(int delta, int health, int maxHealth, bool isCrit = false) {
      Delta = delta;
      Health = health;
      MaxHealth = maxHealth;
      IsCrit = isCrit;
    }
  }

  public class Damageable : MonoBehaviour {
    [SerializeField] int Health;
    [SerializeField] Attributes Attributes;
    [SerializeField] UnityEvent<DamageEvent> OnDamage;
    [SerializeField] UnityEvent OnDeath;

    void Start() {
      var maxHealth = (int)Attributes.GetValue(AttributeTag.Health, 0);
      BroadcastMessage("OnSpawn", new DamageEvent(0, Health, maxHealth, false), SendMessageOptions.DontRequireReceiver);
    }

    void OnHurt(HitParams hitParams) {
      var didCrit = hitParams.CritRoll;
      var damage = (int)hitParams.GetDamage(didCrit);
      TakeDamage(damage, didCrit);
    }

    public void TakeDamage(float damage, bool didCrit) {
      Health = Mathf.Max(0, Health - (int)damage);
      if (Health <= 0) {
        OnDeath.Invoke();
        BroadcastMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
      } else {
        var maxHealth = (int)Attributes.GetValue(AttributeTag.Health, 0);
        var damageEvent = new DamageEvent((int)damage, Health, maxHealth, didCrit);
        OnDamage.Invoke(damageEvent);
        BroadcastMessage("OnDamage", damageEvent, SendMessageOptions.DontRequireReceiver);
      }
    }
  }
}