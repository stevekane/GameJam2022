using TMPro;
using UnityEngine;

public class UpgradeCardUI : MonoBehaviour {
  public TextMeshProUGUI Title;

  public void Init(UpgradeDescription descr) {
    Title.text = descr.Title;
  }
}
