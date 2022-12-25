using System.Threading.Tasks;
using UnityEngine;

public class Explosion : MonoBehaviour {
  public TriggerEvent Hitbox;
  public Timeval Duration = Timeval.FromMillis(500);
  TaskScope MainScope = new();

  void Start() => MainScope.Start(MainAction);
  void OnDestroy() => MainScope.Dispose();

  async Task MainAction(TaskScope scope) {
    if (TryGetComponent(out Hitter hitter))
      await scope.Any(Waiter.Delay(Duration), HitHandler.Loop(Hitbox, hitter.HitParams));
  }
}