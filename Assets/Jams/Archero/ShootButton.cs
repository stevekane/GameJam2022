using UnityEngine;

namespace Archero {
  public class ShootButton : SimpleAbility {
    public Projectile ProjectilePrefab;
    public Vector3 ShootOffset;
    public HitConfig HitConfig;
    public AbilityAction Main;

    void Awake() {
      Main.Listen(MainAction);
    }

    Attributes Attributes => AbilityManager.GetComponent<Attributes>();
    void MainAction() {
      Projectile.Fire(ProjectilePrefab, transform.position + ShootOffset, transform.rotation, Attributes, HitConfig);
    }
  }
}