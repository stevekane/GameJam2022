using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public class MeleeAttackAbility : ClassicAbility {
  [SerializeField] LocalTime LocalTime;
  [SerializeField] PlayableDirector PlayableDirector;

  public override Task MainAction(TaskScope scope) {
    return PlayableDirector.PlayTask(scope, LocalTime);
  }
}