using System;
using System.Collections.Generic;
using UnityEngine;

public static class AudioExtensions {
  public static void Play(this AudioSource source, AudioClip clip) {
    source.Stop();
    source.clip = clip;
    source.Play();
  }
}

public class AudioManager : SingletonBehavior<AudioManager> {
  [SerializeField] AudioClip BackgroundMusic;

  public AudioSource MusicSource;
  public AudioSource SoundSource;
  public float SoundCooldown = .05f;

  void Start() {
    MusicSource.Play(BackgroundMusic);
  }

  public void ResetBackgroundMusic() {
    MusicSource.Play(BackgroundMusic);
  }

  Dictionary<AudioClip, float> SoundLastPlayed = new();
  public void PlaySoundWithCooldown(AudioClip clip) {
    if (!clip) return;
    var lastPlayed = SoundLastPlayed.GetValueOrDefault(clip);
    if (Time.time < lastPlayed + SoundCooldown)
      return;
    SoundLastPlayed[clip] = Time.time;
    SoundSource.PlayOneShot(clip);
  }
}