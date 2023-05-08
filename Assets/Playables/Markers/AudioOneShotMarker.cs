using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[CustomStyle("AudioOneShotMarker")]
public class AudioOneShotMarker : Marker, INotification {
  public AudioClip Clip;
  public PropertyName id => new();
}