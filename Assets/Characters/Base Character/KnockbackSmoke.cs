using UnityEngine;

public class KnockbackSmoke : MonoBehaviour {
  [SerializeField] Status Status;
  [SerializeField] ParticleSystem ParticleSystem;
  [SerializeField] float ParticleSize = 1f;

  void FixedUpdate() {
    if (Status.IsHurt) {
      ParticleSystem.Emit(1);
    }
  }
}