using UnityEngine;
using UnityEngine.Events;

namespace Archero {
  public class Damageable : MonoBehaviour {
    [SerializeField] int Health;
    [SerializeField] UnityEvent OnDamage;
    [SerializeField] UnityEvent OnDeath;

    void OnHurt(HitParams hitParams) {
      var didCrit = hitParams.CritRoll;
      var damage = hitParams.GetDamage(didCrit);
      OnDamage?.Invoke();  // Guessing this is meant as an on-hit callback?
      TakeDamage(damage, didCrit);
    }

    // Things like DoTs go through here.
    public void TakeDamage(float damage, bool didCrit) {
      Debug.Log($"Hit {name} for {damage} {(didCrit ? "CRIT" : "")}");
      Health = Mathf.Max(0, Health - (int)damage);
      if (Health <= 0) {
        OnDeath?.Invoke();
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
      }
    }
  }
}