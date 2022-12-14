using UnityEngine;

public class KnockbackSmoke : MonoBehaviour {
  [SerializeField] Status Status;
  [SerializeField] ParticleSystem ParticleSystem;

  void FixedUpdate() {
    if (Status.IsHurt) {
      ParticleSystem.Emit(1);
    }
  }
}