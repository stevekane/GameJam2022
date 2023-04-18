using System.Threading.Tasks;
using UnityEngine;

public class MeleeAttackAbility : ClassicAbility {
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] LogicalTimeline LogicalTimeline;

  public override async Task MainAction(TaskScope scope) {
    await LogicalTimeline.Play(scope, TimelineTaskConfig);
  }
}