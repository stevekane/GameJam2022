using System.Threading.Tasks;
using UnityEngine;

public class MeleeAttackAbility : ClassicAbility {
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] LogicalTimeline LogicalTimeline;
  [SerializeField] SimpleCharacterController CharacterController;

  public override async Task MainAction(TaskScope scope) {
    await LogicalTimeline.Play(scope, TimelineTaskConfig);
  }
}