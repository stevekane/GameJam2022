using System;
using System.Threading.Tasks;
using UnityEngine;

public class Teleport : Ability {
  [SerializeField] AnimationJobConfig Animation;
  [SerializeField] Flash Flash;
  [SerializeField] AvatarTransform FXTransform;
  [SerializeField] GameObject ChannelVFX;
  [SerializeField] AudioClip OutSFX;
  [SerializeField] GameObject OutVFX;
  [SerializeField] GameObject InVFX;
  [SerializeField] Vector3 VFXOffset;

  public Vector3 Destination { get; set; }

  [NonSerialized] AnimationJob AnimationJob;

  static InlineEffect TeleportEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "Teleport Windup");

  public override async Task MainAction(TaskScope scope) {
    using var effect = Status.Add(TeleportEffect);
    try {
      AnimationJob = AnimationDriver.Play(scope, Animation);
      VFXManager.Instance.TrySpawnWithParent(ChannelVFX, FXTransform.Transform, Animation.Clip.length);
      await AnimationJob.WaitDone(scope);
      SFXManager.Instance.TryPlayOneShot(OutSFX);
      VFXManager.Instance.TrySpawnEffect(OutVFX, AbilityManager.transform.position+VFXOffset);
      VFXManager.Instance.TrySpawnEffect(InVFX, Destination+VFXOffset);
      Flash.Run();
      Mover.Move(Destination-AbilityManager.transform.position);
    } finally {}
  }
}