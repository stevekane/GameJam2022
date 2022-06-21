using UnityEngine;

public class VictoryBox : MonoBehaviour {
  void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.TryGetComponent(out Hero player)) {
      Debug.Log("VIctory!");
      Invoke("Replay", 1);
    }
  }

  void Replay() {
    var ed = FindObjectOfType<EventDriver>();
    if (ed.PlayState != PlayState.PlayBack)
      ed.PlaybackScene();
  }
}
