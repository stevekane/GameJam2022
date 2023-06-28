using System;
using UnityEngine;
using UnityEngine.Events;

namespace Archero {
  [Serializable]
  public struct DamageEvent {
    public int Delta;
    public int OldHealth;
    public int NewHealth;
  }

  public class Damageable : MonoBehaviour {
    [SerializeField] int Health;
    [SerializeField] UnityEvent<DamageEvent> OnDamage;
    [SerializeField] UnityEvent OnDeath;

    void OnHurt(HitParams hitParams) {
      var newHealth = Mathf.Max(0, Health - (int)hitParams.Damage);
      var damageEvent = new DamageEvent {
        Delta = (int)hitParams.Damage,
        OldHealth = Health,
        NewHealth = newHealth
      };
      Health = newHealth;
      OnDamage.Invoke(damageEvent);
      // TODO: Move damage text to separate system that subs to OnDamage
      var damageText = $"-{hitParams.Damage}";
      var damageTextPosition = transform.position+2*Vector3.up;
      var damageMessage = WorldSpaceMessageManager.Instance.SpawnMessage(damageText, damageTextPosition);
      damageMessage.LocalScale = Vector3.one;
      Destroy(damageMessage.gameObject, 2);
      if (Health <= 0) {
        OnDeath.Invoke();
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
      }
    }
  }
}