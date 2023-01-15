using System.Threading.Tasks;
using UnityEngine;

public class AnimationPhaseTest : Ability {
  public AnimationJobConfig AnimationConfig;

  public AnimationJob Animation;
  public int Which = 0;
  int Frame = 0;
  int Phase = 0;
  AnimationEventListener Events;

  public void Start() {
    Events = AbilityManager.GetComponentInChildren<AnimationEventListener>();
  }

  public override async Task MainAction(TaskScope scope) {
    switch (Which) {
    case 0: await One(scope); break;
    case 1: await Two(scope); break;
    }
  }
  public async Task One(TaskScope scope) {
    try {
      if (Animation == null)
        Animation = AnimationDriver.Play(AbilityManager.MainScope, AnimationConfig);
      if (Phase > AnimationConfig.Clip.events.Length) {
        Debug.Log($"Animation done");
        Animation.Stop();
        Animation = null;
        Phase = 0;
        return;
      }
      Animation.Resume();
      await Animation.WaitPhase(Phase++)(scope);
      Animation.Pause();
      Debug.Log($"Now at {Phase} = {Animation.CurrentFrame} / {Animation.NumFrames}");
    } finally {
    }
  }

  public async Task Two(TaskScope scope) {
    try {
      Animation = AnimationDriver.Play(scope, AnimationConfig);
      using var listener = new ScopedListener<int>(Events.Event, async a => {
        Animation.Pause();
        Debug.Log($"Pausing at {a} = {Animation.CurrentFrame} / {Animation.NumFrames}");
        await scope.Millis(1000);
        Animation.Resume();
      });
      await Animation.WaitDone(scope);
    } finally {
    }
  }
}