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
      //Debug.Log($"Hit {name} for {damage} {(didCrit ? "CRIT" : "")}");
      Health = Mathf.Max(0, Health - (int)damage);
      OnDamage?.Invoke();
      if (Health <= 0) {
        OnDeath?.Invoke();
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
      }
    }
  }
}