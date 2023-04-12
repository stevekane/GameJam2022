using TMPro;
using UnityEngine;

public class RadialMenuUIItem : MonoBehaviour {
  public TextMeshProUGUI Title;

  public void Init(string title) {
    Title.text = title;
  }
}
