using UnityEngine;

namespace Traditional {
  public class LocalTimeScale : AttributeFloat {
    public override float Base { get; set; } = 1;

    void OnDrawGizmosSelected() {
      var center = transform.position;
      var color = Color.Lerp(Color.red, Color.white, Value);
      Gizmos.DrawIcon(center, "Clock.png", false, color);
    }
  }
}