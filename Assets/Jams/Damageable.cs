using UnityEngine;
using UnityEngine.Events;

namespace Archero {
  public class Damageable : MonoBehaviour {
    [SerializeField] int Health;
    [SerializeField] UnityEvent OnDamage;
    [SerializeField] UnityEvent OnDeath;

    void OnHurt(HitParams hitParams) {
      Health = Mathf.Max(0, Health - (int)hitParams.Damage);
      OnDamage?.Invoke();
      if (Health <= 0) {
        OnDeath?.Invoke();
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
      }
    }
  }
}