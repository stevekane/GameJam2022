using UnityEngine;

public class Sniper : MonoBehaviour {
  AbilityManager AbilityManager;
  TaskScope Scope = new();

  void Awake() {
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => Scope.Start(Waiter.Repeat(async scope => {
    var abilities = GetComponentsInChildren<Ability>();
    var index = UnityEngine.Random.Range(0, abilities.Length);
    await scope.Run(AbilityManager.TryRun(abilities[index].MainAction));
    await scope.Tick();
  }));
  void OnDestroy() => Scope.Dispose();
}