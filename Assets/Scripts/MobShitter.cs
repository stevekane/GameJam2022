using UnityEngine;

public class MobShitter : Mob {
  public Bullet BulletPrefab;
  Player Player;

  enum MobShitterState { Idle, Shoot }

  MobShitterState State {
    get { return (MobShitterState)Animator.GetInteger("State"); }
    set { Animator.SetInteger("State", (int)value); }
  }

  public new void Start() {
    base.Start();
    Player = GameObject.FindObjectOfType<Player>();
    State = MobShitterState.Idle;
  }

  void FixedUpdate() {
    switch (State) {
    case MobShitterState.Idle:
      var playerDelta = (Player.transform.position - transform.position);
      var playerInRange = playerDelta.sqrMagnitude < Config.ShootRadius*Config.ShootRadius;
      if (playerInRange) {
        State = MobShitterState.Shoot;
      }
      break;
    }
  }

  public void Shoot() {
    var playerDir = (Player.transform.position - transform.position).normalized;
    Bullet.Fire(BulletPrefab, transform.position + Vector3.up*.5f + playerDir, playerDir, Bullet.BulletType.STUN, Config.BulletSpeed);
  }

  public void ShootCooldown() {
    State = MobShitterState.Idle;
  }

  public void OnDrawGizmosSelected() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Config.ShootRadius);
  }
}
