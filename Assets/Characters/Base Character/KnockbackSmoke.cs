using UnityEngine;

public class KnockbackSmoke : MonoBehaviour {
  [SerializeField] Status Status;
  [SerializeField] CharacterController Controller;
  [SerializeField] ParticleSystem ParticleSystem;
  [SerializeField] float ParticleSize = 1f;

  void FixedUpdate() {
    if (Status.IsHurt) {
      var emitParams = new ParticleSystem.EmitParams {};
      ParticleSystem.Emit(emitParams, 1);
    }
  }
}