using UnityEngine;
using UnityEngine.Timeline;

[TrackBindingType(typeof(SimpleCharacterController))]
[TrackClipType(typeof(RootMotionClip))]
[TrackClipType(typeof(MotionWarpingClip))]
public class SimpleCharacterControllerTrack : TrackAsset {}