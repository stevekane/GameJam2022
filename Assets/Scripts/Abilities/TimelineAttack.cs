using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class TimelineAttack : Ability {
  [SerializeField] TimelineAsset TimelineAsset;
  [SerializeField] TimelineBindings TimelineBindings;

  public override async Task MainAction(TaskScope scope) {
    try {
      var timeline = AnimationDriver.PlayTimeline(scope, TimelineAsset, TimelineBindings);
      await scope.Until(delegate { return timeline.IsDone(); });
    } finally {
      Debug.Log("Timeline done");
    }
  }
}