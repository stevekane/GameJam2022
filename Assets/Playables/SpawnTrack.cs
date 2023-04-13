using UnityEngine;
using UnityEngine.Timeline;

[TrackBindingType(typeof(GameObject))]
[TrackClipType(typeof(SpawnClip))]
public class SpawnTrack : TrackAsset {}