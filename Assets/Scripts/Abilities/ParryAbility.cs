using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ParryAbility : Ability {
  [SerializeField] Hurtbox Hurtbox;
  [SerializeField] Timeval BlockDuration = Timeval.FromAnimFrames(15, 30);
  [SerializeField] MeleeAbility RiposteAbility;
  Animator Animator;

  public static InlineEffect Invulnerable { get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "ParryInvulnerable");
  }

  public override async Task MainAction(TaskScope scope) {
    if (AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude > 0f)
      return;
    try {
      using (Status.Add(Invulnerable)) {
        Animator.SetBool("Blocking", true);
        Debug.LogWarning("Fix up Parry Ability to listen for hurtbox event");
        var onHurt = Waiter.ListenFor(Hurtbox.OnHurt);
        var hurt = await scope.Any(onHurt, Waiter.Return<HitParams>(Waiter.Delay(BlockDuration)), Waiter.Return<HitParams>(ListenFor(MainRelease)));
        if (hurt != null)
          AbilityManager.MainScope.Start(Riposte);
      }
    } finally {
      Animator.SetBool("Blocking", false);
    }
  }

  public async Task Riposte(TaskScope scope) {
    using (Status.Add(Invulnerable)) {
      AbilityManager.TryInvoke(RiposteAbility.MainAction);
      await scope.While(() => RiposteAbility.IsRunning);
    }
  }

  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
  }
}
