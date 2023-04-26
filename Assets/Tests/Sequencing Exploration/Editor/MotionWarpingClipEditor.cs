using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor.Timeline;

[CustomTimelineEditor(typeof(MotionWarpingClip))]
public class MotionWarpingClipEditor : ClipEditor {
  public override void OnClipChanged(TimelineClip clip) {
    var motionWarpingClip = (MotionWarpingClip)clip.asset;
    var seconds = clip.duration;
    var ticks = Mathf.RoundToInt((float)(Timeval.FixedUpdatePerSecond * seconds));
    clip.displayName = $"Motion Warp {ticks}";
    motionWarpingClip.Ticks = ticks;
    base.OnClipChanged(clip);
  }
}