using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Traditional {
  public class MoveSpeed : AttributeFloat {
    [field:SerializeField]
    public override float Base { get; set; } = 12;

    #if UNITY_EDITOR
    void OnDrawGizmosSelected() {
      var v = Value;
      if (v != 0 && EditorApplication.isPlaying) {
        var t = (float)Time.time % 1;
        Gizmos.DrawLine(transform.position, v * t * transform.forward + transform.position);
      }
    }
    #endif
  }
}