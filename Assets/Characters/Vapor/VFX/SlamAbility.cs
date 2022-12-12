using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SlamAbility : Ability {
  Animator Animator;
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

  AnimationJobTask Animation;

  void Start() {
    Animator = GetComponentInParent<Animator>();
  }

  public async Task ChargeStart(TaskScope scope) {
    try {
      Animation = AnimationDriver.Play(scope, Clip);
      using var effect = new SpeedFactorEffect(.5f, .5f);
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

  public async Task ChargeRelease(TaskScope scope) {
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
    hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject));
  }
}