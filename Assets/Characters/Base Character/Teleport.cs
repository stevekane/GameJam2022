using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class Teleport : Ability {
  [SerializeField] AnimationJobConfig Animation;
  [SerializeField] Flash Flash;
  [SerializeField] AvatarTransform FXTransform;
  [SerializeField] GameObject ChannelVFX;
  [SerializeField] AudioClip OutSFX;
  [SerializeField] GameObject OutVFX;
  [SerializeField] AudioClip InSFX;
  [SerializeField] GameObject InVFX;
  [SerializeField] Vector3 VFXOffset;
  [SerializeField] float Distance;

  public Vector3 Destination { get; set; }

  static InlineEffect TeleportEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
    s.HasGravity = false;
  }, "Teleport Windup");

  public override async Task MainAction(TaskScope scope) {
    await scope.Any(ListenFor(MainRelease));
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ.TryGetDirection();
    if (!dir.HasValue)
      return;
    using var effect = Status.Add(TeleportEffect);
    var animationJob = AnimationDriver.Play(scope, Animation);
    VFXManager.Instance.TrySpawnWithParent(ChannelVFX, FXTransform.Transform, Animation.Clip.length);
    await animationJob.WaitDone(scope);
    SFXManager.Instance.TryPlayOneShot(OutSFX);
    VFXManager.Instance.TrySpawnEffect(OutVFX, AbilityManager.transform.position+VFXOffset);
    SFXManager.Instance.TryPlayOneShot(InSFX);
    VFXManager.Instance.TrySpawnEffect(InVFX, Destination+VFXOffset);
    Flash.Run();
    Destination = transform.position + dir.Value*Distance;
    Mover.Teleport(Destination);
    await scope.Tick(); // Important: Nothing should happen on this frame once the teleport concludes
  }
  public override Task MainRelease(TaskScope scope) => null;
}