using System.Collections;
using UnityEngine;

namespace PigMoss {
  class RadialBurst : FiberAbility {
    public Vibrator Vibrator;
    public GameObject ProjectilePrefab;
    public AudioClip FireSFX;
    public Timeval ChargeDelay;
    public Timeval FireDelay;
    public int Count;
    public int Rotations;

    StatusEffect StatusEffect;

    public override float Score() {
      if (BlackBoard.DistanceScore < 25 && BlackBoard.DistanceScore > 5) {
        return Mathf.InverseLerp(5, 25, BlackBoard.DistanceScore);
      } else {
        return 0;
      }
    }

    public override void OnStop() {
      Status.Remove(StatusEffect);
    }

    public override IEnumerator Routine() {
      StatusEffect = new InlineEffect(status => {
        status.CanMove = false;
        status.CanRotate = false;
      });
      Status.Add(StatusEffect);
      Vibrator.Vibrate(Vector3.up, ChargeDelay.Ticks, 1f);
      yield return Fiber.Wait(ChargeDelay);
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
        }
        yield return Fiber.Wait(FireDelay);
        direction = halfRotationPerProjectile*direction;
      }
    }
  }
}