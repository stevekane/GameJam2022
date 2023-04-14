using System.Threading.Tasks;
using UnityEngine;

public class SlideAblity : SimpleAbility {
  [SerializeField] LogicalTimeline LogicalTimeline;
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] float Distance = 10;

  TaskScope Scope;
  float Duration;

  public override void OnRun() {
    Scope = new();
    Scope.Run(Slide);
  }

  public override void OnStop() {
    Scope.Dispose();
    Scope = null;
  }

  void FixedUpdate() {
    if (!IsRunning)
      return;
    var velocity = (Distance / Duration) * transform.forward;
    DirectMotion.IsActive(true, 1);
    DirectMotion.Override(velocity, 1);
  }

  async Task Slide(TaskScope scope) {
    try {
      IsRunning = true;
      Duration = (float)(TimelineTaskConfig.Asset.TickDuration(Timeval.FixedUpdatePerSecond)+1);
      await LogicalTimeline.Play(scope, TimelineTaskConfig);
      Stop();
    } finally {
      IsRunning = false;
    }
  }
}