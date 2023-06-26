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
  [SerializeField] float PositionWeight = 1;
  [SerializeField] float RotationWeight;
  [SerializeField] float AnkleHeight = .5f;
  [SerializeField] float Speed = 1;

  PlayableGraph Graph;
  AnimationClipPlayable FootbasePlayable;
  AnimationPlayableOutput Output;

  [ContextMenu("SampleAsset")]
  public void SampleAsset() {
    Asset?.Sample();
  }

  void Start() {
    GetComponentInChildren<RootMotionListener>().IKCallback += OnIK;
    GetComponentInChildren<RootMotionListener>().OnRootMotion += OnMove;
    GetComponentInChildren<RootMotionListener>().OnRootRotation += OnRotate;
    Graph = PlayableGraph.Create("Focus Tracking");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    FootbasePlayable = AnimationClipPlayable.Create(Graph, Asset.AnimationClip);
    FootbasePlayable.SetApplyPlayableIK(true);
    Output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    Output.SetSourcePlayable(FootbasePlayable);
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void SetIK(AvatarIKGoal goal, Vector3 p, Quaternion q) {
    Animator.SetIKPosition(goal, p);
    Animator.SetIKPositionWeight(goal, PositionWeight);
    Animator.SetIKRotation(goal, q);
    Animator.SetIKRotationWeight(goal, RotationWeight);
  }

  void OnIK() {
    var fraction = (float)FootbasePlayable.GetTime() / FootbasePlayable.GetAnimationClip().length;
    var index = Mathf.FloorToInt(Asset.FrameCount * fraction) % Asset.FrameCount;
    var lfp = Asset.LeftFootBases[index] + AnkleHeight * Vector3.up;
    var rfp = Asset.RightFootBases[index] + AnkleHeight * Vector3.up;
    var lfr = Quaternion.LookRotation(Asset.LeftFootDirections[index], Vector3.up);
    var rfr = Quaternion.LookRotation(Asset.RightFootDirections[index], Vector3.up);
    var leftFoot = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
    var leftToes = Animator.GetBoneTransform(HumanBodyBones.LeftToes);
    var leftHeel = leftFoot.TransformPoint(Asset.leftHeelLocalPosition);
    var leftToe = leftToes.TransformPoint(Asset.leftToeLocalPosition);
    Debug.DrawLine(leftHeel, leftToe, Color.magenta);

    // SetIK(AvatarIKGoal.LeftFoot, lfp, lfr);
    // SetIK(AvatarIKGoal.RightFoot, rfp, rfr);
    Debug.DrawRay(Animator.GetIKPosition(AvatarIKGoal.LeftFoot), Vector3.up, Color.black);
    Debug.DrawRay(Animator.GetIKPosition(AvatarIKGoal.RightFoot), Vector3.up, Color.black);
  }

  void OnMove(Vector3 deltaPosition) {
    transform.Translate(deltaPosition);
  }

  void OnRotate(Quaternion deltaRotation) {
    transform.rotation = Animator.deltaRotation * transform.rotation;
  }

  float LeftFootDeltaTime;
  float RightFootDeltaTime;
  Vector3 LeftFootNext;
  Vector3 RightFootNext;
  void PredictFootSteps() {
    var animSeconds = (float)FootbasePlayable.GetTime();
    var animDuration = (float)FootbasePlayable.GetAnimationClip().length;
    var cycleTime = animSeconds / animDuration;
    var leftFootDownTime = Asset.LeftFoot.Stride.StanceCycleTime;
    var rightFootDownTime = Asset.RightFoot.Stride.StanceCycleTime;
    var leftFootDeltaTime = FootBaseAsset.Cyclic(leftFootDownTime-cycleTime);
    var rightFootDeltaTime = FootBaseAsset.Cyclic(rightFootDownTime-cycleTime);
    var leftFootSeconds = leftFootDeltaTime * animDuration;
    var rightFootSeconds = rightFootDeltaTime * animDuration;
    var leftFootRotation = Asset.LeftFoot.Stride.StanceRotation;
    var rightFootRotation = Asset.RightFoot.Stride.StanceRotation;
    if (LeftFootDeltaTime < leftFootDeltaTime) {
      Debug.Log("Left step");
      LeftFootNext
        = transform.position
        + animDuration * Asset.Cycle.Speed * Asset.Cycle.Direction.XZ()
        + Asset.LeftFoot.Stride.StancePosition;
    }
    LeftFootDeltaTime = leftFootDeltaTime;
    if (RightFootDeltaTime < rightFootDeltaTime) {
      Debug.Log("Right step");
      RightFootNext
        = transform.position
        + animDuration * Asset.Cycle.Speed * Asset.Cycle.Direction.XZ()
        + Asset.RightFoot.Stride.StancePosition;
    }
    RightFootDeltaTime = rightFootDeltaTime;

    Debug.DrawRay(LeftFootNext, leftFootRotation, Color.blue);
    Debug.DrawRay(RightFootNext, rightFootRotation, Color.green);
  }

  void FixedUpdate() {
    // var totalFrames = Asset.LeftFootPositions.Length;
    // var time = Mathf.Lerp(0, Asset.AnimationClip.length, Mathf.InverseLerp(0, totalFrames-1, FrameIndex%totalFrames));
    // FootbasePlayable.SetTime(time);
    PredictFootSteps();
    Graph.Evaluate(Time.fixedDeltaTime * Speed);
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

  // void OnDrawGizmos() {
  //   if (!Asset && Asset.FrameCount > 0)
  //     return;
  //   var index = FrameIndex % Asset.LeftFootDirections.Length;

  //   TraceFootBaseInWorldSpace();

  //   if (ShowStridePath) {
  //     Gizmos.color = Color.black;
  //     Gizmos.DrawLine(transform.TransformPoint(Asset.LeftExtent1), transform.TransformPoint(Asset.LeftExtent2));
  //     Gizmos.DrawLine(transform.TransformPoint(Asset.RightExtent1), transform.TransformPoint(Asset.RightExtent2));
  //     Gizmos.DrawRay(transform.TransformPoint(Asset.LeftExtent2), Asset.LeftAxis);
  //     Gizmos.DrawRay(transform.TransformPoint(Asset.RightExtent2), Asset.RightAxis);
  //   }

  //   if (ShowCyclePositions) {
  //     Gizmos.color = Color.white;
  //     var lfposition = transform.TransformPoint(Asset.LeftFoot.Stride.StancePosition);
  //     var lfdirection = transform.TransformVector(Asset.LeftFoot.Stride.StanceRotation);
  //     Gizmos.DrawRay(lfposition, lfdirection);

  //     var rfposition = transform.TransformPoint(Asset.RightFoot.Stride.StancePosition);
  //     var rfdirection = transform.TransformVector(Asset.RightFoot.Stride.StanceRotation);
  //     Gizmos.DrawRay(rfposition, rfdirection);
  //   }

  //   if (ShowFootBase) {
  //     var lfposition = transform.TransformPoint(Asset.LeftFootBases[index]);
  //     var lfdirection = Asset.LeftFootDirections[index];
  //     Gizmos.color = Color.blue;
  //     Gizmos.DrawRay(lfposition, lfdirection);

  //     var rfposition = transform.TransformPoint(Asset.RightFootBases[index]);
  //     // TODO: This probably should transform this vector
  //     var rfdirection = Asset.RightFootDirections[index];
  //     Gizmos.color = Color.green;
  //     Gizmos.DrawRay(rfposition, rfdirection);
  //   }
  // }
}