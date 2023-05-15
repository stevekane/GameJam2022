using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public class SlideAblity : ClassicAbility {
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] PlayableDirector PlayableDirector;
  [SerializeField] float Distance = 10;

  public override async Task MainAction(TaskScope scope) {
    var duration = (float)PlayableDirector.playableAsset.duration;
    var velocity = (Distance / duration) * transform.forward;
    await scope.Any(
      PlayableDirector.PlayTask(LocalTime),
      Waiter.Repeat(() => CharacterController.Move(velocity)));
  }
}