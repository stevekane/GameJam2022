using UnityEngine;

public class SFXManager : MonoBehaviour {
  public static SFXManager Instance;

  [SerializeField] AudioSource AudioSource;

  void Awake() {
    Instance = this;
  }

  public bool TryPlayOneShot(AudioClip clip) {
    return AudioSource.PlayOptionalOneShot(clip);
  }
}