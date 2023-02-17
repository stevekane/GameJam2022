using UnityEngine;
using UnityEngine.Timeline;

public class TimelineMiner : MonoBehaviour {
  [SerializeField] TimelineAsset TimelineAsset;

  void Start() {
    foreach (var rootTrackAsset in TimelineAsset.GetRootTracks()) {
      foreach (var timelineClip in rootTrackAsset.GetClips()) {
        var playableAsset = timelineClip.asset;
        var type = playableAsset.GetType();
        if (playableAsset is LogTrackAsset) {
          var logTrackAsset = (LogTrackAsset)playableAsset;
          Debug.Log($"Log {logTrackAsset.Message} for {timelineClip.start} -> {timelineClip.end}");
        }
      }
    }
  }
}