using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineTest : Ability {
  public TimelineAsset Timeline;

  public override async Task MainAction(TaskScope scope) {
    try {
      var playable = AnimationDriver.PlayTimeline(scope, Timeline);
      await scope.Until(() => playable.IsDone());
    } finally {
      Debug.Log("Done");
    }
  }
}