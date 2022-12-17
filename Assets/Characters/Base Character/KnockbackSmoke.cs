using UnityEngine;

public class KnockbackSmoke : MonoBehaviour {
  [SerializeField] Status Status;
  [SerializeField] Mover Mover;
  [SerializeField] ParticleSystem ParticleSystem;
  [SerializeField] float MinimumSpeed = 10;

  void FixedUpdate() {
    if (Status.IsHurt && !Status.IsGrounded && Mover.Velocity.sqrMagnitude >= MinimumSpeed*MinimumSpeed) {
      ParticleSystem.Emit(1);
    }
  }
}