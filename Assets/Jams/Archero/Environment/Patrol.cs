using UnityEngine;

namespace Archero {
  public class Patrol : MonoBehaviour {
    [SerializeField] Waypoints Waypoints;
    [SerializeField] float Speed = 1;

    float direction = 1;
    float interpolant = 0;

    void FixedUpdate() {
      interpolant += direction * Speed * Time.fixedDeltaTime;
      direction = interpolant <= 0 ? 1 : interpolant >= 1 ? -1 : direction;
      interpolant = Mathf.Clamp01(interpolant);
      var pathData = Waypoints.ToWorldSpace(interpolant);
      transform.position = pathData.Position;
      transform.rotation = pathData.Rotation;
    }
  }
}