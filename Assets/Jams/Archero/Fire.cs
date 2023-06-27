using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Fire : ClassicAbility {
    public Targeting Targeting;
    public Projectile ProjectilePrefab;
    public Vector3 ShootOffset;
    public HitConfig HitConfig;
    public Timeval VolleyPeriod = Timeval.FromMillis(100);
    public int CooldownTicks;
    public int DefaultVolleyCount = 1;
    public int DefaultForwardArrowCount = 1;
    public int DefaultDiagonalArrowCount = 0;
    public int DefaultSidewaysArrowCount = 0;
    public int DefaultRearArrowCount = 0;
    public float SpreadDistance = .15f;

    Attributes Attributes => AbilityManager.GetComponent<Attributes>();

    void FixedUpdate() {
      if (CooldownTicks > 0) {
        CooldownTicks--;
      } else if (AbilityManager.CanRun(Main)) {
        AbilityManager.Run(Main);
      }
    }

    void FireArrowVolley(Vector3 direction, int count) {
      var spreadDirection = Vector3.Cross(direction, Vector3.up);
      var spreadTotal = count == 0 ? 0 : (count-1) * SpreadDistance;
      var spreadHalf = spreadTotal / 2;
      for (var i = 0; i < count; i++) {
        var origin = transform.position + ShootOffset + (i * SpreadDistance - spreadHalf) * spreadDirection;
        Projectile.Fire(ProjectilePrefab, origin, Quaternion.LookRotation(direction), Attributes, HitConfig);
      }
    }

    public override async Task MainAction(TaskScope scope) {
      var attacksPerSecond = Attributes.GetValue(AttributeTag.AttackSpeed, 0);
      var secondsPerAttack = 1f/attacksPerSecond;
      CooldownTicks = Mathf.RoundToInt(secondsPerAttack*Timeval.FixedUpdatePerSecond);
      var target = Targeting.BestTarget;
      var volleys = Attributes.GetValue(AttributeTag.Multishot, DefaultVolleyCount);
      try {
        if (target && volleys > 0) {
          var toTarget = target.transform.position-transform.position;
          var toRightSide = Vector3.Cross(toTarget, Vector3.up);
          var toLeftSide = -toRightSide;
          var toRightDiagonal = (toTarget + toRightSide).normalized;
          var toLeftDiagonal = (toTarget + toLeftSide).normalized;
          var toRear = -toTarget;
          var frontArrowCount = (int)Attributes.GetValue(AttributeTag.FrontArrow, DefaultForwardArrowCount);
          var sideArrowCount = (int)Attributes.GetValue(AttributeTag.SideArrow, DefaultSidewaysArrowCount);
          var diagonalArrowCount = (int)Attributes.GetValue(AttributeTag.DiagonalArrow, DefaultDiagonalArrowCount);
          var rearArrowCount = (int)Attributes.GetValue(AttributeTag.RearArrow, DefaultRearArrowCount);
          for (var i = 0; i < volleys; i++) {
            await scope.Ticks(VolleyPeriod.Ticks);
            FireArrowVolley(toTarget, frontArrowCount);
            FireArrowVolley(toRightSide, sideArrowCount);
            FireArrowVolley(toLeftSide, sideArrowCount);
            FireArrowVolley(toRightDiagonal, diagonalArrowCount);
            FireArrowVolley(toLeftDiagonal, diagonalArrowCount);
            FireArrowVolley(toRear, rearArrowCount);
          }
        }
      } catch (Exception e) {
        Debug.LogError(e);
      } finally {
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