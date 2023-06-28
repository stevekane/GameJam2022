using UnityEngine;
using UnityEngine.UI;

namespace Archero {
  public class Healthbar : MonoBehaviour {
    [SerializeField] Damageable Damageable;
    [SerializeField] Attributes Attributes;
    [SerializeField] Slider Slider;
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] Vector3 Offset;

    public void OnDamage(DamageEvent damageEvent) {
      Debug.Log("Fired");
      Slider.value = (float)damageEvent.NewHealth / Attributes.GetValue(AttributeTag.Health, 0);
    }

    void LateUpdate() {
      transform.rotation = PersonalCamera.Current.transform.rotation;
      transform.localPosition = Offset;
    }
  }
}