using TMPro;
using UnityEngine;

namespace Archero {
  public class UpgradeCardUI : MonoBehaviour {
    public TextMeshProUGUI Title;

    public void Init(UpgradeDescription descr) {
      Title.text = descr.Title;
    }
  }
}