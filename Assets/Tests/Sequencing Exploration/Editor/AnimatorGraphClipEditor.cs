using UnityEngine.Timeline;
using UnityEditor.Timeline;

[CustomTimelineEditor(typeof(AnimatorGraphClip))]
public class AnimatorGraphClipEditor : ClipEditor {
  public override void OnClipChanged(TimelineClip clip) {
    var animatorGraphClip = (AnimatorGraphClip)clip.asset;
    clip.displayName = animatorGraphClip.Clip?.name;
    base.OnClipChanged(clip);
  }

  public override ClipDrawOptions GetClipOptions(TimelineClip clip) {
    var animatorGraphClip = (AnimatorGraphClip)clip.asset;
    return new() {
      tooltip = $"{animatorGraphClip.Clip.length} seconds"
    };
  }
}