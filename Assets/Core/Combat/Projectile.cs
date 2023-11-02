using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour {
  public LayerMask LayerMask;
  public QueryTriggerInteraction TriggerInteraction;
  public Vector3 PreviousPosition;
  public float InitialSpeed = 10;

  void Start() {
    ProjectileManager.Instance.AddProjectile(this);
    GetComponent<Rigidbody>().AddForce(InitialSpeed*transform.forward, ForceMode.Impulse);
  }

  void OnDestroy() {
    ProjectileManager.Instance.RemoveProjectile(this);
  }
}