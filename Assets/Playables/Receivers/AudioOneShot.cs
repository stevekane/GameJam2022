using UnityEngine;
using UnityEngine.Playables;

public class AudioOneShot : MonoBehaviour, INotificationReceiver {
  [SerializeField] AudioSource AudioSource;

  public void OnNotify(Playable playable, INotification notification, object context) {
    if (notification is AudioOneShotMarker oneShot)
      AudioSource.PlayOneShot(oneShot.Clip);
  }
}