using UnityEngine;

public class MobMove : MonoBehaviour { }

public class MobMoveCircles : MobMove {
  MobConfig Config;
  Vector3 Tangent;

  void Start() {
    Config = GetComponent<Mob>().Config;
    Tangent = transform.right;
  }

  void Update() {
    transform.position += Config.MoveSpeed * Time.deltaTime * Tangent;
    Tangent = Quaternion.Euler(0, Config.TurnSpeedDeg * Time.deltaTime, 0) * Tangent;
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.green;
    Gizmos.DrawLine(transform.position, transform.position + Tangent * 5f);
  }
}
