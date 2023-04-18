using System.Threading.Tasks;
using UnityEngine;

public class SlideAblity : SimpleAbility {
  public AbilityAction Slide;

  [SerializeField] LogicalTimeline LogicalTimeline;
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] float Distance = 10;

  void Awake() {
    Slide.Ability = this;
    Slide.CanRun = true;
    Slide.Source.Listen(Main);
  }

  TaskScope Scope;
  float Duration;

  void Main() {
    Scope = new();
    Scope.Run(SlideTask);
  }

  public override void Stop() {
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

  async Task SlideTask(TaskScope scope) {
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