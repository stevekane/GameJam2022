using System.Threading.Tasks;
using UnityEngine;

public class BackDash : Ability {
  [SerializeField] float Distance = 5f;
  [SerializeField] AnimationJobConfig Animation;
  [SerializeField] GameObject LaunchVFX;
  [SerializeField] AudioClip LaunchSFX;
  [SerializeField] GameObject LandVFX;
  [SerializeField] AudioClip LandSFX;

  public static InlineEffect ScriptedMove => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.IsDamageable = false;
    s.IsHittable = false;
  }, "DashMove");

  public override async Task MainAction(TaskScope scope) {
    Debug.Assert(Status.CanAttack, "Dash fired while unable to attack");
    using var scriptedMove = Status.Add(ScriptedMove);
    var animation = AnimationDriver.Play(scope, Animation);
    SFXManager.Instance.TryPlayOneShot(LaunchSFX);
    VFXManager.Instance.TrySpawnEffect(LaunchVFX, transform.position);
    await scope.Any(animation.WaitDone, Waiter.Repeat(Move));
    SFXManager.Instance.TryPlayOneShot(LandSFX);
    VFXManager.Instance.TrySpawnEffect(LandVFX, transform.position);
  }

  async Task Move(TaskScope scope) {
    var speed = Distance / Timeval.FromSeconds(Animation.Clip.length).Ticks;
    var inPlane = speed * -transform.forward.XZ();
    Mover.Move(inPlane);
    await scope.Tick();
  }
}