using System.Threading.Tasks;
using UnityEngine;

public class SlideAblity : ClassicAbility {
  [SerializeField] LogicalTimeline LogicalTimeline;
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] float Distance = 10;

  float Duration;

  void FixedUpdate() {
    if (!IsRunning)
      return;
    var velocity = (Distance / Duration) * transform.forward;
    DirectMotion.IsActive(true, 1);
    DirectMotion.Override(velocity, 1);
  }

  public override async Task MainAction(TaskScope scope) {
    Duration = (float)(TimelineTaskConfig.Asset.TickDuration(Timeval.FixedUpdatePerSecond)+1);
    await LogicalTimeline.Play(scope, TimelineTaskConfig);
  }
}