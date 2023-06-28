using System;
using UnityEngine;
using UnityEngine.Events;

namespace Archero {
  [Serializable]
  public struct DamageEvent {
    public int Delta;
    public int Health;
    public bool IsCrit;
    public DamageEvent(int delta, int health, bool isCrit = false) {
      Delta = delta;
      Health = health;
      IsCrit = isCrit;
    }
  }

  public class Damageable : MonoBehaviour {
    [SerializeField] int Health;
    [SerializeField] UnityEvent<DamageEvent> OnDamage;
    [SerializeField] UnityEvent OnDeath;

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
        var damageEvent = new DamageEvent((int)damage, Health, didCrit);
        OnDamage.Invoke(damageEvent);
        BroadcastMessage("OnDamage", damageEvent, SendMessageOptions.DontRequireReceiver);
      }
    }
  }
}