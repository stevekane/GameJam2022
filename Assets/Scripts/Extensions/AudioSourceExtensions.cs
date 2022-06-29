using UnityEngine;

public static class AudioSourceExtensions {
  public static bool PlayOptionalOneShot(this AudioSource source, AudioClip clip) {
    if (clip) {
      source.PlayOneShot(clip);
      return true;
    } else {
      return false;
    }
  }
}
