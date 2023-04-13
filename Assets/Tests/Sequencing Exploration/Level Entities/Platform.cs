using UnityEngine;

public class Platform : MonoBehaviour {
  [SerializeField] float MoveSpeed = 10;
  [SerializeField] Vector3 Velocity;

  void FixedUpdate() {
    Velocity = MoveSpeed * new Vector3(Mathf.Sin(Time.time), 0, Mathf.Cos(Time.time));
    transform.position += Time.deltaTime * Velocity;
  }

  void OnTriggerStay(Collider c) {
    if (c.TryGetComponent(out DirectMotion directMotion)) {
      Debug.Log("Got here");
      directMotion.AddMotion(Velocity * Time.fixedDeltaTime);
    }
  }
}