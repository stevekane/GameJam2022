using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace PigMoss {
  class RadialBurst : Ability {
    public HitConfig HitConfig;
    public Vibrator Vibrator;
    public BigFatSlowBoom ProjectilePrefab;
    public AudioClip FireSFX;
    public Timeval ChargeDelay;
    public Timeval FireDelay;
    public int Count;
    public int Rotations;

    public override float Score() {
      if (BlackBoard.DistanceScore < 25 && BlackBoard.DistanceScore > 5) {
        return Mathf.InverseLerp(5, 25, BlackBoard.DistanceScore);
      } else {
        return 0;
      }
    }

    public async Task Routine(TaskScope scope) {
      using var effect = AddStatusEffect(new InlineEffect(status => {
        status.CanMove = false;
        status.CanRotate = false;
      }));
      Vibrator.Vibrate(Vector3.up, ChargeDelay.Ticks, 1f);
      await scope.Delay(ChargeDelay);
      var rotationPerProjectile = Quaternion.Euler(0, 360/(float)Count, 0);
      var halfRotationPerProjectile = Quaternion.Euler(0, 180/(float)Count, 0);
      var direction = AbilityManager.transform.forward.XZ();
      for (var j = 0; j < Rotations; j++) {
        SFXManager.Instance.TryPlayOneShot(FireSFX);
        for (var i = 0; i < Count; i++) {
          direction = rotationPerProjectile*direction;
          var rotation = Quaternion.LookRotation(direction, Vector3.up);
          var radius = 5;
          var position = AbilityManager.transform.position+radius*direction+Vector3.up;
          var projectile = GameObject.Instantiate(ProjectilePrefab, position, rotation);
          projectile.InitHitParams(HitConfig, GetComponentInParent<Attributes>());
        }
        await scope.Delay(FireDelay);
        direction = halfRotationPerProjectile*direction;
      }
    }
  }
}