using UnityEngine;

public class MobMovePatrol : MobMove {
  public Transform[] Waypoints;
  MobConfig Config;
  int Target = 0;

  void Start() {
    Config = GetComponent<Mob>().Config;
  }

  void Update() {
    var delta = Waypoints[Target].position - transform.position;
    if (delta.sqrMagnitude < .1) {
      Target = (Target + 1) % Waypoints.Length;  // TODO: pause?
    } else {
      transform.position += Config.MoveSpeed * Time.deltaTime * delta.normalized;
      transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);  // TODO: animate
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.green;
    for (int i = 0; i < Waypoints.Length; i++)
      Gizmos.DrawLine(Waypoints[i].position, Waypoints[(i + 1) % Waypoints.Length].position);
  }
}