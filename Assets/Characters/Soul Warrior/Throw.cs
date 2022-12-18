using System;
using System.Threading.Tasks;
using UnityEngine;

public class Throw : Ability {
  [SerializeField] AttributeModifier MoveSpeedModifier = AttributeModifier.TimesOne;
  [SerializeField] AnimationJobConfig Animation;
  [SerializeField] Timeval Windup = Timeval.FromAnimFrames(20, 60);
  [SerializeField] AvatarTransform Spawn;
  [SerializeField] GameObject ChannelVFX;
  [SerializeField] AudioSource ChannelAudioSource;
  [SerializeField] GameObject ProjectilePrefab;

  public Action<GameObject> OnThrow;

  InlineEffect ThrowEffect => new(s => {
    s.AddAttributeModifier(AttributeTag.MoveSpeed, MoveSpeedModifier);
    s.CanAttack = false;
  }, "Throwing");

  public override async Task MainAction(TaskScope scope) {
    using var effect = Status.Add(ThrowEffect);
    try {
      VFXManager.Instance.TrySpawnWithParent(ChannelVFX, Spawn.Transform, Windup.Seconds);
      ChannelAudioSource.Play();
      var animation = AnimationDriver.Play(scope, Animation);
      await animation.WaitFrame(Windup.AnimFrames)(scope);
      var position = Spawn.Transform.position;
      var rotation = AbilityManager.transform.rotation;
      var projectile = Instantiate(ProjectilePrefab, position, rotation);
      OnThrow?.Invoke(projectile);
      await animation.WaitDone(scope);
    } finally {
      ChannelAudioSource.Stop();
    }
  }
}