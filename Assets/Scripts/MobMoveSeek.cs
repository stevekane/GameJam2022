using UnityEngine;

public class MobMoveSeek : MonoBehaviour {
  public MobConfig Config;
  Player Player;

  private void Start() {
    Player = GameObject.FindObjectOfType<Player>();
  }

  void Update() {
    var playerDelta = (Player.transform.position - transform.position);
    var playerInRange = playerDelta.sqrMagnitude < Config.SeekRadius*Config.SeekRadius;
    if (playerInRange) {
      transform.position += Config.MoveSpeed * Time.deltaTime * playerDelta.normalized;
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.yellow;
    Gizmos.DrawWireSphere(transform.position, Config.SeekRadius);
  }
}
