using UnityEngine;

[DefaultExecutionOrder(2)]
public class TestPlatform : MonoBehaviour {
  [SerializeField] float MoveSpeed = 10;
  [SerializeField] float Offset;

  public TestPlatformPart[] Parts;
  public Vector3 Velocity;
  public EventSource<Vector3> OnMove = new();

  void FixedUpdate() {
    var t = Time.time;
    var dt = Time.deltaTime;
    var v = MoveSpeed * new Vector3(0, Mathf.Sin(t + Offset), 0);
    var dp = dt * v;
    Velocity = v;
    Parts.ForEach(part => part.Rigidbody.MovePosition(part.Rigidbody.position + dp));
    OnMove.Fire(dp);
  }
}