using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class KnockdownAbility : ClassicAbility {
  [Header("Components")]
  [SerializeField] PlayableDirector PlayableDirector;
  [SerializeField] LocalTime LocalTime;
  [Header("Config")]
  [SerializeField] TimelineAsset KnockdownTimeline;
  [SerializeField] AnimationClip GroundedLoopClip;
  [SerializeField] TimelineAsset GetupTimeline;
  [SerializeField] Timeval Duration = Timeval.FromSeconds(1);

  // play the first clip
  // loop the grounded clip
  // play the second clip
  public override async Task MainAction(TaskScope scope) {
    try {
      PlayableDirector.playableAsset = KnockdownTimeline;
      await PlayableDirector.PlayTask(LocalTime)(scope);
      // TODO: Figure out looping
      PlayableDirector.playableAsset = GetupTimeline;
      await PlayableDirector.PlayTask(LocalTime)(scope);
    } finally {

    }
  }
}