using UnityEngine;

public class MobMoveSeek : MobMove {
  MobConfig Config;
  Player Player;

  void Start() {
    Config = GetComponent<Mob>().Config;
    Player = GameObject.FindObjectOfType<Player>();
  }

  public override void Step(float dt) {
    var playerDelta = (Player.transform.position - transform.position).XZ();
    var playerInRange = playerDelta.sqrMagnitude < Config.SeekRadius*Config.SeekRadius;
    var playerInShootRange = playerDelta.sqrMagnitude < Config.ShootRadius*Config.ShootRadius;
    if (playerInRange && !playerInShootRange) {
      transform.position += Config.MoveSpeed * dt * playerDelta.normalized;
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.yellow;
    Gizmos.DrawWireSphere(transform.position, GetComponent<Mob>().Config.SeekRadius);
  }
}
