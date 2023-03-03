using System.ComponentModel;
using System.Threading.Tasks;
using UnityEngine;

public class SpawnGrappler : MonoBehaviour {
  public AbilityManager Character;
  public GrapplePoint GrappleTarget;
  public Timeval DespawnAfter = Timeval.FromSeconds(3);

  TaskScope MainScope = new();
  void Start() => MainScope.Start(Waiter.Repeat(Sequence));

  async Task Sequence(TaskScope scope) {
    var instance = Instantiate(Character, transform.position, transform.rotation);
    instance.gameObject.SetActive(true);
    await scope.Millis(500);
    var ability = instance.GetComponentInChildren<Grapple>();
    ability.ScriptedTarget = GrappleTarget;
    await instance.TryRun(scope, ability.MainAction);
    await scope.Delay(DespawnAfter);
    Destroy(instance.gameObject);
  }
}
