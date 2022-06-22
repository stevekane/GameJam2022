using UnityEngine;

public class MobShitter : Mob {
  public Bullet BulletPrefab;
  Hero Player;
  float TimeRemaining;

  enum StateType { Idle, Shoot, Cooldown }
  StateType State = StateType.Idle;

  public new void Start() {
    base.Start();
    Player = GameObject.FindObjectOfType<Hero>();
  }

  void FixedUpdate() {
    switch (State) {
    case StateType.Idle:
      var playerDelta = (Player.transform.position - transform.position);
      var playerInRange = playerDelta.sqrMagnitude < Config.ShootRadius*Config.ShootRadius;
      if (playerInRange) {
        TimeRemaining = 55f/60f;
        State = StateType.Shoot;
      }
      break;
    case StateType.Shoot:
      TimeRemaining -= Time.fixedDeltaTime;
      if (TimeRemaining < 0f)
        Shoot();
      break;
    case StateType.Cooldown:
      TimeRemaining -= Time.fixedDeltaTime;
      if (TimeRemaining < 0f)
        State = StateType.Idle;
      break;
    }
    if (Player.LegTarget?.gameObject == gameObject)
      State = StateType.Idle;

    Animator.SetInteger("State", State == StateType.Shoot ? 1 : 0);
  }

  public void Shoot() {
    var playerDir = (Player.transform.position - transform.position).XZ().normalized;
    Bullet.Fire(BulletPrefab, transform.position + Vector3.up*.5f + playerDir, playerDir, Bullet.BulletType.STUN, Config.BulletSpeed);
    TimeRemaining = Config.ShootCooldown;
    State = StateType.Cooldown;
  }

  public void OnDrawGizmosSelected() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Config.ShootRadius);
  }
}
