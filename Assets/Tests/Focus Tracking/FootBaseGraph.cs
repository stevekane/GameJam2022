using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class FootBaseGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] FootBaseAsset Asset;
  [SerializeField, Range(0, 100)] int FrameIndex;
  [Header("Options")]
  [SerializeField] bool ShowCyclePositions;
  [SerializeField] bool ShowStridePath;
  [SerializeField] bool ShowFootBase;

  PlayableGraph Graph;
  AnimationClipPlayable FootbasePlayable;
  AnimationPlayableOutput Output;

  [ContextMenu("SampleAsset")]
  public void SampleAsset() {
    Asset?.Sample();
  }

  void Start() {
    Graph = PlayableGraph.Create("Focus Tracking");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    FootbasePlayable = AnimationClipPlayable.Create(Graph, Asset.AnimationClip);
    Output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    Output.SetSourcePlayable(FootbasePlayable);
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void FixedUpdate() {
    var totalFrames = Asset.LeftFootPositions.Length;
    var time = Mathf.Lerp(0, Asset.AnimationClip.length, Mathf.InverseLerp(0, totalFrames-1, FrameIndex%totalFrames));
    FootbasePlayable.SetTime(time);
    Graph.Evaluate();
  }

  void TraceFootBaseInWorldSpace() {
    for (var i = 0; i < Asset.FrameCount; i++) {
      {
      var footBase = Asset.LeftFoot.FootBase[i].TranslationOffset;
      var footDirection = Asset.LeftFootDirections[i];
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(footBase, footDirection);
      }
      {
      var footBase = Asset.RightFoot.FootBase[i].TranslationOffset;
      var footDirection = Asset.RightFootDirections[i];
      Gizmos.color = Color.green;
      Gizmos.DrawRay(footBase, footDirection);
      }
    }
  }

  void OnDrawGizmos() {
    if (!Asset && Asset.FrameCount > 0)
      return;
    var index = FrameIndex % Asset.LeftFootDirections.Length;

    TraceFootBaseInWorldSpace();

    if (ShowStridePath) {
      Gizmos.color = Color.black;
      Gizmos.DrawLine(transform.TransformPoint(Asset.LeftExtent1), transform.TransformPoint(Asset.LeftExtent2));
      Gizmos.DrawLine(transform.TransformPoint(Asset.RightExtent1), transform.TransformPoint(Asset.RightExtent2));
      Gizmos.DrawRay(transform.TransformPoint(Asset.LeftExtent2), Asset.LeftAxis);
      Gizmos.DrawRay(transform.TransformPoint(Asset.RightExtent2), Asset.RightAxis);
    }

    if (ShowCyclePositions) {
      Gizmos.color = Color.white;
      var lfposition = transform.TransformPoint(Asset.LeftFoot.Stride.StancePosition);
      var lfdirection = transform.TransformVector(Asset.LeftFoot.Stride.StanceRotation);
      Gizmos.DrawRay(lfposition, lfdirection);

      var rfposition = transform.TransformPoint(Asset.RightFoot.Stride.StancePosition);
      var rfdirection = transform.TransformVector(Asset.RightFoot.Stride.StanceRotation);
      Gizmos.DrawRay(rfposition, rfdirection);
    }

    if (ShowFootBase) {
      var lfposition = transform.TransformPoint(Asset.LeftFootBases[index]);
      var lfdirection = Asset.LeftFootDirections[index];
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(lfposition, lfdirection);

      var rfposition = transform.TransformPoint(Asset.RightFootBases[index]);
      // TODO: This probably should transform this vector
      var rfdirection = Asset.RightFootDirections[index];
      Gizmos.color = Color.green;
      Gizmos.DrawRay(rfposition, rfdirection);
    }
  }
}