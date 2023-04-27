using UnityEngine;
using UnityEngine.Timeline;

[TrackBindingType(typeof(SimpleAbility))]
[TrackClipType(typeof(ModifyFlagsClip))]
public class SimpleAbilityTrack : TrackAsset {}