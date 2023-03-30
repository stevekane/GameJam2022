using UnityEngine;

namespace ActionsAndAI {
  public class SimplePhysicsProjectile : MonoBehaviour {
    [SerializeField] float Speed = 25;

    void Awake() {
      GetComponent<Rigidbody>()?.AddForce(Speed * transform.forward, ForceMode.Impulse);
    }
  }
}