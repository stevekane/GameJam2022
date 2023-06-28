using UnityEngine;

namespace Archero {
  public class DamageNumberManager : MonoBehaviour {
    void OnDamage(DamageEvent damageEvent) {
      var damageText = $"-{damageEvent.Delta}";
      var damageTextPosition = transform.position+2*Vector3.up;
      var damageMessage = WorldSpaceMessageManager.Instance.SpawnMessage(damageText, damageTextPosition);
      damageMessage.LocalScale = (damageEvent.IsCrit ? 1.5f : 1) * Vector3.one;
      Destroy(damageMessage.gameObject, 2);
    }
  }
}