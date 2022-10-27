using UnityEngine;

public class SFXManager : MonoBehaviour {
  public static SFXManager Instance;

  [SerializeField] AudioSource AudioSource;

  public bool TryPlayOneShot(AudioClip clip) {
    return AudioSource.PlayOptionalOneShot(clip);
  }
}