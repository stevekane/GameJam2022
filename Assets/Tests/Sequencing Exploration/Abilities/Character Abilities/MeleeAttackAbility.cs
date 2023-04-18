using System.Threading.Tasks;
using UnityEngine;

public class MeleeAttackAbility : SimpleAbility {
  public AbilityAction Attack;

  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] LogicalTimeline LogicalTimeline;

  void Awake() {
    Attack.Ability = this;
    Attack.CanRun = true;
    Attack.Source.Listen(Main);
  }

  TaskScope Scope;

  void Main() {
    IsRunning = true;
    Scope = new();
    Scope.Run(MainAction);
  }

  public override void Stop() {
    IsRunning = false;
    Scope.Dispose();
    Scope = null;
  }

  async Task MainAction(TaskScope scope) {
    await LogicalTimeline.Play(scope, TimelineTaskConfig);
    Stop();
  }
}