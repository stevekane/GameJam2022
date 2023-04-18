using System.Threading.Tasks;
using UnityEngine;

public class SlideAblity : ClassicAbility {
  [SerializeField] LogicalTimeline LogicalTimeline;
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] float Distance = 10;

  public override async Task MainAction(TaskScope scope) {
    var duration = (float)(TimelineTaskConfig.Asset.TickDuration(Timeval.FixedUpdatePerSecond)+1);
    var velocity = (Distance / duration) * transform.forward;
    await scope.Any(
      s => LogicalTimeline.Play(s, TimelineTaskConfig),
      Waiter.Repeat(delegate {
        DirectMotion.IsActive(true, 1);
        DirectMotion.Override(velocity, 1);
      }));
  }
}