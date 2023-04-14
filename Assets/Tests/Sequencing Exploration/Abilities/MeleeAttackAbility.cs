using System.Threading.Tasks;
using UnityEngine;

public class MeleeAttackAbility : SimpleAbility {
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] LogicalTimeline LogicalTimeline;

  TaskScope Scope;

  public override void OnRun() {
    Scope = new();
    Scope.Run(Attack);
  }

  public override void OnStop() {
    Scope.Dispose();
    Scope = null;
  }

  async Task Attack(TaskScope scope) {
    try {
      IsRunning = true;
      await LogicalTimeline.Play(scope, TimelineTaskConfig);
      Stop();
    } finally {
      IsRunning = false;
    }
  }
}