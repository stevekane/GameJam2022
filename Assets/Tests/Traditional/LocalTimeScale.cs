using UnityEngine;

namespace Traditional {
  public class LocalTimeScale : Attribute<float> {
    [SerializeField] Vector3 GizmoOffset = 4*Vector3.up;
    public override float Base { get; set; } = 1;
    public override float Evaluate(float t) {
      return t;
    }

    void OnDrawGizmosSelected() {
      var center = GizmoOffset + transform.position;
      var color = Color.Lerp(Color.red, Color.white, Evaluate(Base));
      Gizmos.DrawIcon(center, "Clock.png", false, color);
    }
  }
}