using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Archero {
  public class Healthbar : MonoBehaviour {
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] Slider Slider;
    [SerializeField] TextMeshProUGUI HealthText;
    [SerializeField] Vector3 Offset;

    void OnSpawn(DamageEvent damageEvent) {
      Slider.value = (float)damageEvent.Health / damageEvent.MaxHealth;
      HealthText.text = damageEvent.Health.ToString();
    }

    void OnDamage(DamageEvent damageEvent) {
      Slider.value = (float)damageEvent.Health / damageEvent.MaxHealth;
      HealthText.text = damageEvent.Health.ToString();
    }

    //void OnDeath() {
    //  Slider.gameObject.SetActive(false);
    //  HealthText.gameObject.SetActive(false);
    //}

    void LateUpdate() {
      transform.rotation = PersonalCamera.Current.transform.rotation;
      transform.localPosition = Offset;
    }
  }
}