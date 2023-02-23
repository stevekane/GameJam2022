using System.Threading.Tasks;
using UnityEngine;

public class ShieldAbility : Ability {
  public int Index;
  public AnimationJobConfig BlockAnimation;
  public Shield Shield;

  public static InlineEffect Invulnerable => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "ShieldInvulnerable");

  public override async Task MainAction(TaskScope scope) {
    AnimationTask animation = null;
    try {
      animation = AnimationDriver.Play(scope, BlockAnimation);
      await animation.PauseAfterPhase(scope, 0);
      if (Shield)
        Shield.HurtboxEnabled = true;
      using (Status.Add(Invulnerable)) {
        await scope.ListenFor(AbilityManager.GetEvent(MainRelease));
      }
    } finally {
      if (Shield)
        Shield.HurtboxEnabled = false;
    }
    animation.Resume();
    await animation.WaitDone(scope);
  }
}