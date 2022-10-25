using TMPro;
using UnityEngine;

public class UpgradeCardUI : MonoBehaviour {
  public TextMeshProUGUI Title;
  public TextMeshProUGUI CurrentEffect;
  public TextMeshProUGUI NextEffect;
  public TextMeshProUGUI CurrentLevel;
  public TextMeshProUGUI Cost;

  public void Init(UpgradeDescription descr) {
    Title.text = descr.Title;
    CurrentEffect.text = descr.CurrentEffect;
    NextEffect.text = descr.NextEffect;
    CurrentLevel.text = $"Level: {descr.CurrentLevel}";
    Cost.text = descr.Cost switch {
      0 => "FREE",
      int.MaxValue => "",
      _ => $"${descr.Cost}",
    };
  }
}
