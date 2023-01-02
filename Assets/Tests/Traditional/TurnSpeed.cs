using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Traditional {
  public class TurnSpeed : Attribute<float> {
    [field:SerializeField]
    public override float Base { get; set; } = 360;
    public override float Evaluate(float t) {
      return t;
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected() {
      var v = Evaluate(Base);
      if (v != 0 && EditorApplication.isPlaying) {
        var t = (float)Time.time;
        var period = 360 / v;
        var degrees = 360 * ((t / period) % 1);
        var rotation = Quaternion.Euler(0, degrees, 0);
        Gizmos.DrawLine(transform.position, rotation*transform.forward + transform.position);
      }
    }
    #endif
  }
}