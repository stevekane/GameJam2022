using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor.Timeline;

[CustomTimelineEditor(typeof(MeleeAimAssistClip))]
public class MeleeAimAssistClipEditor : ClipEditor {
  public override void OnClipChanged(TimelineClip clip) {
    var aimAssistClip = (MeleeAimAssistClip)clip.asset;
    var seconds = clip.duration;
    var ticks = Mathf.RoundToInt((float)(Timeval.FixedUpdatePerSecond * seconds));
    clip.displayName = ticks.ToString();
    aimAssistClip.TotalTicks = ticks;
    base.OnClipChanged(clip);
  }
}