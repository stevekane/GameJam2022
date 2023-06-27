using UnityEngine;

namespace Archero {
  public class Fire : SimpleAbility {
    public Projectile ProjectilePrefab;
    public Vector3 ShootOffset;
    public HitConfig HitConfig;
    public AbilityAction Main;
    public int CooldownTicks;

    Attributes Attributes => AbilityManager.GetComponent<Attributes>();

    void Start() {
      Main.Listen(RunMain);
    }

    void FixedUpdate() {
      if (CooldownTicks > 0) {
        CooldownTicks--;
      } else if (AbilityManager.CanRun(Main)) {
        AbilityManager.Run(Main);
      }
    }

    void RunMain() {
      var attacksPerSecond = Attributes.GetValue(AttributeTag.AttackSpeed, 0);
      var secondsPerAttack = 1f/attacksPerSecond;
      CooldownTicks = Mathf.RoundToInt(secondsPerAttack*Timeval.FixedUpdatePerSecond);
      Projectile.Fire(ProjectilePrefab, transform.position + ShootOffset, transform.rotation, Attributes, HitConfig);
    }
  }
}