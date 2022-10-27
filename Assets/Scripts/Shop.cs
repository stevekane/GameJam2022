using UnityEngine;

public class Shop : MonoBehaviour {
  public UpgradeUI UI;
  public Upgrade[] UpgradeChoices;
  public bool IsOpen { get => UI.IsShowing; }
  void OnTriggerEnter(Collider c) {
    if (c.GetComponent<Player>())
      UI.Show(UpgradeChoices);
  }
  public void Open() {
    UI.Show(UpgradeChoices);
  }
}
