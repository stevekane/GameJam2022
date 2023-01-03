using UnityEngine;

namespace Traditional {
  public class LocalTimeScale : AttributeFloat {
    [SerializeField] Vector3 GizmoOffset = 4*Vector3.up;
    public override float Base { get; set; } = 1;

    void OnDrawGizmosSelected() {
      var center = GizmoOffset + transform.position;
      var color = Color.Lerp(Color.red, Color.white, Value);
      Gizmos.DrawIcon(center, "Clock.png", false, color);
    }
  }
}