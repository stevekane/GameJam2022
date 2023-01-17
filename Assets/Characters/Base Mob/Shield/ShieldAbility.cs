using System.Threading.Tasks;

public class ShieldAbility : Ability {
  public int Index;
  public AnimationJobConfig BlockAnimation;
  public Timeval WindupDuration;
  public Timeval RecoveryDuration;
  public Shield Shield;

  public static InlineEffect Invulnerable => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "ShieldInvulnerable");

  public override async Task MainAction(TaskScope scope) {
    AnimationJob animation = null;
    try {
      animation = AnimationDriver.Play(scope, BlockAnimation);
      await animation.PauseAtFrame(scope, animation.NumFrames-1);
      await scope.Delay(WindupDuration);
      if (Shield)
        Shield.HurtboxEnabled = true;
      using (Status.Add(Invulnerable)) {
        await scope.ListenFor(AbilityManager.GetEvent(MainRelease));
      }
      if (Shield)
        Shield.HurtboxEnabled = false;
      animation.Stop();
      await scope.Delay(RecoveryDuration);
    } finally {
      if (Shield)
        Shield.HurtboxEnabled = false;
    }
  }
}