using UnityEngine;
using UnityEngine.Playables;

public class FootBase : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] FootBaseAsset Asset;
  [SerializeField, Range(0,100)] int FrameIndex;

  PlayableGraph Graph;

  void OnDrawGizmos() {
    if (!Asset && Asset.LeftFootDirections.Length > 0)
      return;
    Gizmos.color = Color.white;
    Gizmos.DrawLine(transform.TransformPoint(Asset.LeftExtent1), transform.TransformPoint(Asset.LeftExtent2));
    Gizmos.DrawLine(transform.TransformPoint(Asset.RightExtent1), transform.TransformPoint(Asset.RightExtent2));
    Gizmos.color = Color.white;
    Gizmos.DrawRay(transform.TransformPoint(Asset.LeftExtent2), Asset.LeftAxis);
    Gizmos.DrawRay(transform.TransformPoint(Asset.RightExtent2), Asset.RightAxis);

    var index = FrameIndex % Asset.LeftFootDirections.Length;
    var lfposition = transform.TransformPoint(Asset.LeftFootBases[index]);
    var lfdirection = Asset.LeftFootDirections[index];
    Gizmos.color = Color.blue;
    Gizmos.DrawRay(lfposition, lfdirection);

    var rfposition = transform.TransformPoint(Asset.RightFootBases[index]);
    var rfdirection = Asset.RightFootDirections[index];
    Gizmos.color = Color.green;
    Gizmos.DrawRay(rfposition, rfdirection);
  }
}