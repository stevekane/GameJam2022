using System.Threading.Tasks;
using UnityEngine;

public class Throw : Ability {
  [SerializeField] AttributeModifier MoveSpeedModifier = AttributeModifier.TimesOne;
  [SerializeField] AnimationJobConfig Animation;
  [SerializeField] Timeval Windup = Timeval.FromAnimFrames(20, 60);
  [SerializeField] AvatarBone Spawn;
  [SerializeField] GameObject ChannelVFX;
  [SerializeField] AudioSource ChannelAudioSource;
  [SerializeField] GameObject SpawnVFX;
  [SerializeField] AudioClip SpawnSFX;
  [SerializeField] Hitter ProjectilePrefab;
  [SerializeField] HitConfig HitConfig;

  public Transform Target { get; set; }
  Transform SpawnTransform => AvatarAttacher.GetBoneTransform(Spawn);

  public override HitConfig HitConfigData => HitConfig;

  InlineEffect ThrowEffect => new(s => {
    s.AddAttributeModifier(AttributeTag.MoveSpeed, MoveSpeedModifier);
    s.CanAttack = false;
  }, "Throwing");

  public override async Task MainAction(TaskScope scope) {
    using var effect = Status.Add(ThrowEffect);
    try {
      VFXManager.Instance.TrySpawnWithParent(ChannelVFX, SpawnTransform, Windup.Seconds);
      ChannelAudioSource.Play();
      var animation = AnimationDriver.Play(scope, Animation);
      await animation.WaitFrame(Windup.AnimFrames)(scope);
      var dir = (Target ? AbilityManager.transform.position.TryGetDirection(Target.position) : null) ?? AbilityManager.transform.forward;
      var rotation = Quaternion.LookRotation(dir);
      var projectile = Instantiate(ProjectilePrefab, SpawnTransform.position, rotation);
      projectile.HitParams = new(HitConfig, Attributes.SerializedCopy, Attributes.gameObject, projectile.gameObject);
      SFXManager.Instance.TryPlayOneShot(SpawnSFX);
      VFXManager.Instance.TrySpawnEffect(SpawnVFX, SpawnTransform.position, rotation);
      await animation.WaitDone(scope);
    } finally {
      ChannelAudioSource.Stop();
    }
  }
}