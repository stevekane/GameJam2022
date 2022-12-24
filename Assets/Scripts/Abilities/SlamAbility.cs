using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SlamAbility : Ability {
  public AnimationClip Clip;
  public Timeval WindupDuration;
  public Timeval SlamPiecePeriod;
  public GameObject SlamActionPrefab;
  SlamAction SlamAction;
  public HitConfig HitConfig;
  public GameObject FireVFX;
  public AudioClip FireSFX;
  public Vector3 FireVFXOffset;
  public float ChargeSpeedFactor = 1f/6f;

  AnimationJob Animation;

  static readonly AttributeModifier Half = new() { Mult = .5f };
  public static InlineEffect SlowedMove => new(s => {
    s.AddAttributeModifier(AttributeTag.MoveSpeed, Half);
    s.AddAttributeModifier(AttributeTag.TurnSpeed, Half);
  }, "SlamMove");

  public override async Task MainAction(TaskScope scope) {
    try {
      Animation = AnimationDriver.Play(scope, Clip);
      using var effect = Status.Add(SlowedMove);
      Animation.SetSpeed(ChargeSpeedFactor);
      await scope.Any(Charging, Animation.WaitFrame(WindupDuration.AnimFrames));
      Animation.SetSpeed(1f);
      SlamAction.Activate();
      SFXManager.Instance.TryPlayOneShot(FireSFX);
      VFXManager.Instance.TrySpawnEffect(FireVFX, SlamAction.Piece.transform.position + FireVFXOffset);
      SlamAction = null;
      await Animation.WaitDone(scope);
    } finally {
      Animation?.Stop();
      Animation = null;
      if (SlamAction != null) {
        Destroy(SlamAction.gameObject);
        SlamAction = null;
      }
    }
  }

  public override async Task MainRelease(TaskScope scope) {
    Animation?.SetSpeed(1f);
    await scope.Yield();
  }

  async Task Charging(TaskScope scope) {
    int frames = 0;
    var slam = Instantiate(SlamActionPrefab, transform, false);
    slam.layer = gameObject.layer;
    SlamAction = slam.GetComponent<SlamAction>();
    SlamAction.OnHit = OnHit;
    while (true) {
      if (--frames <= 0) {
        SlamAction.AddPiece();
        frames = SlamPiecePeriod.Ticks;
      }
      await scope.Tick();
    }
  }

  void OnHit(Hurtbox hurtbox) {
    hurtbox.TryAttack(new HitParams(HitConfig, Attributes));
  }
}