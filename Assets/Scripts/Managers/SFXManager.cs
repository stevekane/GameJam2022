using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour {
  public static SFXManager Instance;

  public AudioClip FallSFX;

  [SerializeField] AudioSource AudioSource;
  List<AudioClip> ClipsPlayedThisFrame = new();  // TODO: is this a good idea?

  void FixedUpdate() {
    ClipsPlayedThisFrame.Clear();
  }

  public bool TryPlayOneShot(AudioClip clip) {
    if (ClipsPlayedThisFrame.Contains(clip))
      return false;
    ClipsPlayedThisFrame.Add(clip);
    return AudioSource.PlayOptionalOneShot(clip);
  }
}