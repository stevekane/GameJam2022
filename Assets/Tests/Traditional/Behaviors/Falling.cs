using UnityEngine;

namespace Traditional {
  public class Falling : MonoBehaviour {
    [SerializeField] LocalTimeScale LocalTimeScale;
    [SerializeField] Grounded Grounded;
    [SerializeField] Gravity Gravity;
    [SerializeField] FallSpeed FallSpeed;
    [SerializeField] MaxFallSpeed MaxFallSpeed;

    void FixedUpdate() {
      if (!Grounded.Value) {
        var dt = LocalTimeScale.Value * Time.fixedDeltaTime;
        var unboundedFallSpeed = dt * Gravity.Value + FallSpeed.Value;
        var boundedFallSpeed = Mathf.Max(MaxFallSpeed.Value, unboundedFallSpeed);
        FallSpeed.Base = boundedFallSpeed;
      }
    }
  }
}