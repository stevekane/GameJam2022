using System.Threading.Tasks;
using UnityEngine;

public class Throw : Ability {
  [SerializeField] AttributeModifier MoveSpeedModifier = AttributeModifier.TimesOne;
  [SerializeField] AnimationJobConfig Animation;
  [SerializeField] Timeval Windup = Timeval.FromTicks(20);
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
      await animation.WaitFrame(Windup.Ticks)(scope);
      //var dir = (Target ? AbilityManager.transform.position.TryGetDirection(Target.position) : null) ?? ChooseDir();
      var dir = ChooseDir();
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

  public Vector3 ChooseDir() {
    var aim = AbilityManager.GetAxis(AxisTag.Aim);
    var aiming = aim.XZ.sqrMagnitude > 0;
    var direction = aiming ? aim.XZ : transform.forward.XZ();
    var bestScore = float.MaxValue;
    var eye = transform.position;
    Transform candidate = null;
    if (aiming) {
      foreach (var mob in MobManager.Instance.Mobs) {
        var target = mob.transform;
        var isVisible = target.IsVisibleFrom(eye, Defaults.Instance.GrapplePointLayerMask, QueryTriggerInteraction.Collide);
        var dist = Vector3.Distance(transform.position, target.position);
        var angle = Mathf.Abs(Vector3.Angle(direction, (target.position - eye).XZ()));
        var score = angle > 180f ? float.MaxValue : 100f*(angle/180f) + dist;
        //if (isVisible && score < bestScore) {
        if (score < bestScore) {
          candidate = target;
          bestScore = score;
          Debug.Log($"Candidate: {candidate}");
        }
      }
      if (candidate != null) {
        //Status.AddNextTick(s => s.AddAttributeModifier(AttributeTag.LocalTimeScale, AttributeModifier.Times(AimLocalTimeDilation)));
        //GrappleAimLine.SetPosition(1, Candidate.transform.position);
        direction = AbilityManager.transform.position.TryGetDirection(candidate.transform.position) ?? direction;
          Debug.Log($"Candidate chosen: {candidate} {direction}");
      }
    }
    return direction;
  }

}