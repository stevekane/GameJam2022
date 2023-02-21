using System.Threading.Tasks;
using UnityEngine;

public class TimelineAttack : Ability {
  [SerializeField] TimelineTaskConfig Timeline;
  [SerializeField] HitConfig HitConfig;
  TriggerEvent Hitbox;

  void Start() {
    this.InitComponentFromChildren(out Hitbox);
  }

  public override async Task MainAction(TaskScope scope) {
    try {
      var timeline = AnimationDriver.PlayTimeline(scope, Timeline);
      await scope.Any(
        timeline.WaitDone,
        HitHandler.LoopTimeline(Hitbox, null, new HitParams(HitConfig, Attributes), OnHit));
    } finally {
      Debug.Log("Timeline done");
    }
  }

  void OnHit(Hurtbox target) {
    AbilityManager.Energy?.Value.Add(1);
  }
}