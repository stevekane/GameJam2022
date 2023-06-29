using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Archero {
  public class ExperienceBar : MonoBehaviour {
    [SerializeField] TextMeshProUGUI ExperienceText;
    [SerializeField] Slider Slider;

    public void OnLevel(int level) {
      ExperienceText.text = $"Lv.{level}";
    }

    public void OnExperience(ExperienceEvent experienceEvent) {
      Slider.value = Mathf.Clamp01(Mathf.InverseLerp(0, experienceEvent.NextLevelExperience, experienceEvent.Experience));
    }
  }
}