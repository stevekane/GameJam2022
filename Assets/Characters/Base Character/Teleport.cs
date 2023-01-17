using System.Threading.Tasks;
using UnityEngine;

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

  public Vector3 Destination { get; set; }

  static InlineEffect TeleportEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
    s.HasGravity = false;
  }, "Teleport Windup");

  public override async Task MainAction(TaskScope scope) {
    using var effect = Status.Add(TeleportEffect);
    try {
      var animationJob = AnimationDriver.Play(scope, Animation);
      VFXManager.Instance.TrySpawnWithParent(ChannelVFX, FXTransform.Transform, Animation.Clip.length);
      await animationJob.WaitDone(scope);
      SFXManager.Instance.TryPlayOneShot(OutSFX);
      VFXManager.Instance.TrySpawnEffect(OutVFX, AbilityManager.transform.position+VFXOffset);
      SFXManager.Instance.TryPlayOneShot(InSFX);
      VFXManager.Instance.TrySpawnEffect(InVFX, Destination+VFXOffset);
      Flash.Run();
      Mover.Teleport(Destination - transform.position);
      await scope.Tick(); // Important: Nothing should happen on this frame once the teleport concludes
    } finally {}
  }
}