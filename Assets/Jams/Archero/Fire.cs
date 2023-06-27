using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Fire : ClassicAbility {
    public Targeting Targeting;
    public Projectile ProjectilePrefab;
    public Vector3 ShootOffset;
    public HitConfig HitConfig;
    public int CooldownTicks;

    Attributes Attributes => AbilityManager.GetComponent<Attributes>();

    void FixedUpdate() {
      if (CooldownTicks > 0) {
        CooldownTicks--;
      } else if (AbilityManager.CanRun(Main)) {
        AbilityManager.Run(Main);
      }
    }

    public override async Task MainAction(TaskScope scope) {
      var attacksPerSecond = Attributes.GetValue(AttributeTag.AttackSpeed, 0);
      var secondsPerAttack = 1f/attacksPerSecond;
      CooldownTicks = Mathf.RoundToInt(secondsPerAttack*Timeval.FixedUpdatePerSecond);
      var target = Targeting.BestTarget;
      if (target) {
        var toTarget = target.transform.position-transform.position;
        var toTargetRotation = Quaternion.LookRotation(toTarget);
        Projectile.Fire(ProjectilePrefab, transform.position + ShootOffset, toTargetRotation, Attributes, HitConfig);
        await scope.Tick();
      } else {
        await scope.Tick();
      }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      var target = Targeting.BestTarget;
      if (target) {
        currentRotation = Quaternion.LookRotation((target.transform.position-transform.position).normalized);
      }
    }
  }
}