using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class FootBaseGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] FootBaseAsset Asset;
  [SerializeField, Range(0, 100)] int FrameIndex;
  [Header("Options")]
  [SerializeField] bool ShowHeelToe;
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

  void OnDrawGizmos() {
    if (!Asset && Asset.LeftFootDirections.Length > 0)
      return;
    var index = FrameIndex % Asset.LeftFootDirections.Length;

    if (ShowStridePath) {
      Gizmos.color = Color.white;
      Gizmos.DrawLine(transform.TransformPoint(Asset.LeftExtent1), transform.TransformPoint(Asset.LeftExtent2));
      Gizmos.DrawLine(transform.TransformPoint(Asset.RightExtent1), transform.TransformPoint(Asset.RightExtent2));
      Gizmos.color = Color.white;
      Gizmos.DrawRay(transform.TransformPoint(Asset.LeftExtent2), Asset.LeftAxis);
      Gizmos.DrawRay(transform.TransformPoint(Asset.RightExtent2), Asset.RightAxis);
    }

    if (ShowFootBase) {
      var lfposition = transform.TransformPoint(Asset.LeftFootBases[index]);
      var lfdirection = Asset.LeftFootDirections[index];
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(lfposition, lfdirection);

      var rfposition = transform.TransformPoint(Asset.RightFootBases[index]);
      var rfdirection = Asset.RightFootDirections[index];
      Gizmos.color = Color.green;
      Gizmos.DrawRay(rfposition, rfdirection);
    }

    if (ShowHeelToe) {
      var lheel = transform.TransformPoint(Asset.LeftHeelPositions[index]);
      var ltoe = transform.TransformPoint(Asset.LeftToePositions[index]);
      // var lheel = Asset.LeftHeelPositions[index];
      // var ltoe = Asset.LeftToePositions[index];
      Gizmos.color = Color.blue;
      Gizmos.DrawLine(lheel, ltoe);
      // Gizmos.DrawRay(lheel, Vector3.up);

      var rheel = transform.TransformPoint(Asset.RightHeelPositions[index]);
      var rtoe = transform.TransformPoint(Asset.RightToePositions[index]);
      // var rheel = Asset.RightHeelPositions[index];
      // var rtoe = Asset.RightToePositions[index];
      Gizmos.color = Color.green;
      Gizmos.DrawLine(rheel, rtoe);
      // Gizmos.DrawRay(rheel, Vector3.up);
    }
  }
}