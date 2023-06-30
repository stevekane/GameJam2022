using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public enum TargetingStyle {
    TargetPlayer,
    UseLocal,
    UseGlobal
  }

  public enum SpreadStyle {
    Centered,
    Linear
  }

  public class MultiShot : ClassicAbility {
    public Projectile Projectile;
    public HitConfig HitConfig;
    public float WindupSeconds = 1;
    public float RecoverySeconds = 1;
    public TargetingStyle TargetingStyle;
    public SpreadStyle SpreadStyle;
    public Vector3 InitialDirection = Vector3.forward;
    public int ShotCount = 4;
    public float ShotAngleSpacing = 45;

    Vector3 Direction => TargetingStyle switch {
      TargetingStyle.TargetPlayer => Player.Instance.transform.position-transform.position,
      TargetingStyle.UseLocal => transform.TransformVector(InitialDirection),
      _ => InitialDirection
    };

    Vector3 SpreadDirection(Vector3 direction) {
      return SpreadStyle switch {
        SpreadStyle.Centered => Quaternion.Euler(0, -ShotAngleSpacing*(ShotCount-1)/2,0) * direction,
        _ => direction
      };
    }

    public override async Task MainAction(TaskScope scope) {
      try {
        var initialDirection = SpreadDirection(Direction);
        await scope.Ticks(Timeval.FromSeconds(WindupSeconds).Ticks);
        var attributes = AbilityManager.GetComponent<Attributes>();
        for (var i = 0; i < ShotCount; i++) {
          var angle = Quaternion.Euler(0, i * ShotAngleSpacing, 0) * initialDirection;
          Projectile.Fire(Projectile, transform.position+Vector3.up, Quaternion.LookRotation(angle), attributes, HitConfig);
        }
        await scope.Ticks(Timeval.FromSeconds(RecoverySeconds).Ticks);
      } finally {
      }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      currentRotation = Quaternion.LookRotation(Direction);
    }
  }
}