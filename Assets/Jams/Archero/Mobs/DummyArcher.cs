using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class DummyArcher : MonoBehaviour {
    public AbilityActionFieldReference Attack;
    SimpleAbilityManager AbilityManager => GetComponent<SimpleAbilityManager>();

    TaskScope Scope = new();
    void Start() {
      Scope.Start(Waiter.Repeat(Behavior));
    }

    private async Task Behavior(TaskScope scope) {
      AbilityManager.Run(Attack.Value);
      await scope.While(() => Attack.Value.Ability.IsRunning);
      await scope.Seconds(1f);
    }
  }
}