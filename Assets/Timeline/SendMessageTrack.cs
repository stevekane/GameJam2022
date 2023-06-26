using UnityEngine;
using UnityEngine.Timeline;

[TrackBindingType(typeof(GameObject))]
[TrackClipType(typeof(SendMessageClip))]
public class SendMessageTrack : TrackAsset {}