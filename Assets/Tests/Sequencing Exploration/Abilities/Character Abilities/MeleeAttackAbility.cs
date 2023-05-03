using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public class MeleeAttackAbility : ClassicAbility {
  [SerializeField] LocalTime LocalTime;
  [SerializeField] PlayableDirector PlayableDirector;

  public override async Task MainAction(TaskScope scope) {
    try {
      PlayableDirector.RebuildGraph();
      PlayableDirector.time = 0;
      PlayableDirector.Evaluate();
      do {
        await scope.Tick();
        PlayableDirector.time += LocalTime.FixedDeltaTime;
        PlayableDirector.Evaluate();
      } while (PlayableDirector.time < PlayableDirector.duration);
    } catch (Exception e) {
      Debug.LogError(e.Message);
    } finally {
      PlayableDirector.playableGraph.Destroy();
    }
  }
}