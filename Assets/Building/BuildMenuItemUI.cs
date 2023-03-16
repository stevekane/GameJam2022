using TMPro;
using UnityEngine;

public class BuildMenuItemUI : MonoBehaviour {
  public TextMeshProUGUI Title;

  public void Init(string title) {
    Title.text = title;
  }
}
