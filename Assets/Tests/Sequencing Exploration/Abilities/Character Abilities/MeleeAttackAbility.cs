using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public class MeleeAttackAbility : ClassicAbility {
  [SerializeField] PlayableDirector PlayableDirector;

  public override async Task MainAction(TaskScope scope) {
    PlayableDirector.RebuildGraph();
    PlayableDirector.RebindPlayableGraphOutputs();
    PlayableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
    PlayableDirector.time = 0;
    Debug.Log(PlayableDirector.state);
    await scope.Tick();
    // await LogicalTimeline.Play(scope, TimelineTaskConfig);
  }
}