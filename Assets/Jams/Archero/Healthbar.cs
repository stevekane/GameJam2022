using UnityEngine;
using UnityEngine.UI;

namespace Archero {
  public class Healthbar : MonoBehaviour {
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] Attributes Attributes;
    [SerializeField] Slider Slider;
    [SerializeField] Vector3 Offset;

    void OnDamage(DamageEvent damageEvent) {
      Slider.value = (float)damageEvent.Health / Attributes.GetValue(AttributeTag.Health, 0);
    }

    void OnDeath() {
      Slider.gameObject.SetActive(false);
    }

    void LateUpdate() {
      transform.rotation = PersonalCamera.Current.transform.rotation;
      transform.localPosition = Offset;
    }
  }
}