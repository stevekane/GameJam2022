using UnityEngine;

public class MobMoveCircles : MonoBehaviour {
  public float MoveSpeed = 3f;
  public float AngularSpeedDeg = 20f;
  Vector3 Tangent;

  private void Start() {
    Tangent = transform.right;
  }

  void Update() {
    transform.position += MoveSpeed * Time.deltaTime * Tangent;
    Tangent = Quaternion.Euler(0, AngularSpeedDeg * Time.deltaTime, 0) * Tangent;
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.green;
    Gizmos.DrawLine(transform.position, transform.position + Tangent * 5f);
  }
}
