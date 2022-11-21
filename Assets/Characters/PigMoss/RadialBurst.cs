using System;
using System.Collections;
using UnityEngine;

namespace PigMoss {
  [Serializable]
  class RadialBurstConfig {
    public Transform Owner;
    public Vibrator Vibrator;
    public GameObject ProjectilePrefab;
    public AudioClip FireSFX;
    public Timeval ChargeDelay;
    public Timeval FireDelay;
    public int Count;
    public int Rotations;
  }

  class RadialBurst : FiberAbility {
    RadialBurstConfig Config;
    StatusEffect StatusEffect;

    public RadialBurst(AbilityManager manager, RadialBurstConfig config) {
      AbilityManager = manager;
      Config = config;
    }
    public override void OnStop() {
      if (StatusEffect != null) {
        Status.Remove(StatusEffect);
      }
      StatusEffect = null;
    }
    public override IEnumerator Routine() {
      StatusEffect = new InlineEffect(status => {
        status.CanMove = false;
        status.CanRotate = false;
      });
      Status.Add(StatusEffect);
      Config.Vibrator.Vibrate(Vector3.up, Config.ChargeDelay.Ticks, 1f);
      yield return Fiber.Wait(Config.ChargeDelay);
      var rotationPerProjectile = Quaternion.Euler(0, 360/(float)Config.Count, 0);
      var halfRotationPerProjectile = Quaternion.Euler(0, 180/(float)Config.Count, 0);
      var direction = Config.Owner.forward.XZ();
      for (var j = 0; j < Config.Rotations; j++) {
        SFXManager.Instance.TryPlayOneShot(Config.FireSFX);
        for (var i = 0; i < Config.Count; i++) {
          direction = rotationPerProjectile*direction;
          var rotation = Quaternion.LookRotation(direction, Vector3.up);
          var radius = 5;
          var position = Config.Owner.position+radius*direction+Vector3.up;
          GameObject.Instantiate(Config.ProjectilePrefab, position, rotation);
        }
        yield return Fiber.Wait(Config.FireDelay);
        direction = halfRotationPerProjectile*direction;
      }
    }
  }
}