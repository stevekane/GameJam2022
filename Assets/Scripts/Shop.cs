using UnityEngine;

public class Shop : MonoBehaviour {
  public UpgradeUI UI;
  void OnTriggerEnter(Collider c) {
    if (c.GetComponent<Player>())
      UI.Show();
  }
}
