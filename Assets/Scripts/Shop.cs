using UnityEngine;

public class Shop : MonoBehaviour {
  public UpgradeUI UI;
  public Upgrade[] UpgradeChoices;
  void OnTriggerEnter(Collider c) {
    if (c.GetComponent<Player>())
      UI.Show(UpgradeChoices);
  }
}
