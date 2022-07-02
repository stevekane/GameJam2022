using UnityEngine;

public class MobMove : MonoBehaviour {
  Status Status;

  private void Awake() {
    Status = GetComponent<Status>();
  }
  private void FixedUpdate() {
    if (Status.Current == StatusEffect.Types.None)
      Step(Time.fixedDeltaTime);
  }
  public virtual void Step(float dt) { }
}

public class MobMoveCircles : MobMove {
  MobConfig Config;
  Vector3 Tangent;

  void Start() {
    Config = GetComponent<Mob>().Config;
    Tangent = transform.right;
  }

  public override void Step(float dt) {
    transform.position += Config.MoveSpeed * Time.deltaTime * Tangent;
    Tangent = Quaternion.Euler(0, Config.TurnSpeedDeg * dt, 0) * Tangent;
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.green;
    Gizmos.DrawLine(transform.position, transform.position + Tangent * 5f);
  }
}
