using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[Serializable]
public struct StrideFrames {
  public int FootStrike;
  public int FootLand;
  public int Stance;
  public int FootLift;
  public int FootOff;
}

[Serializable]
public struct FootBase {
  public Vector3 TranslationOffset;
  public Quaternion RotationOffset;
  public float Progression;
}

[Serializable]
public struct Stride {
  public Vector3 StancePosition;
  public Vector3 StanceRotation;
  public float StanceDirection;
  public float StanceCycleTime;
  public float FootLiftStrideTime;
  public float FootOffStrideTime;
  public float FootStrikeStrideTime;
  public float FootLandStrideTime;
}

[Serializable]
public struct Foot {
  public StrideFrames StrideFrames;
  public Stride Stride;
  public FootBase[] FootBase;
}

[Serializable]
public struct Cycle {
  public float Distance;
  public float Duration;
  public float Speed;
  public Vector3 Direction;
}

[CreateAssetMenu(menuName = "AnimationGraph/FootBase")]
public class FootBaseAsset : ScriptableObject {
  // From the paper. Takes -Infinity->Infinity to 0-1
  public static float Cyclic(float n) {
    return n - Mathf.Floor(n);
  }

  public static float StrideNormalizedTime(int stanceFrame, int eventFrame, int totalFrames) {
    var delta = (float)(eventFrame-stanceFrame);
    return Cyclic(delta/totalFrames);
  }

  public static Vector3 Average(Vector3[] xs) {
    var v = Vector3.zero;
    for (var i = 0; i < xs.Length; i++)
      v += xs[i];
    return v/xs.Length;
  }

  public static Vector3 FurthestFrom(Vector3[] xs, Vector3 x) {
    var furthestDistance = 0f;
    var furthestPoint = Vector3.zero;
    for (var i = 0; i < xs.Length; i++) {
      var d = Vector3.SqrMagnitude(xs[i]-x);
      if (d >= furthestDistance) {
        furthestDistance = d;
        furthestPoint = xs[i];
      }
    }
    return furthestPoint;
  }

  public static Vector3 Lowest(Vector3[] xs) {
    var v = xs[0];
    for (var i = 0; i < xs.Length; i++) {
      if (xs[i].y < v.y) {
        v = xs[i];
      }
    }
    return v;
  }

  public static Vector3 Highest(Vector3[] xs) {
    var v = xs[0];
    for (var i = 0; i < xs.Length; i++) {
      if (xs[i].y > v.y) {
        v = xs[i];
      }
    }
    return v;
  }

  // BEGIN Heuristic analysis
  // These are analytical techniques from the paper used to derive key times from the animation
  public static float Cost(
  float heelHeight,
  float toeHeight,
  Vector3 footPosition,
  Vector3 averageGroundPosition,
  Vector3 axis,
  float α,
  float β) {
    return
      Mathf.Max(heelHeight, toeHeight) / α +
      Mathf.Abs(Vector3.Dot(footPosition-averageGroundPosition, axis.normalized)) / β;
  }

  static float calculateα(Vector3[] heelPositions, Vector3[] toePositions) {
    var highestHeel = Highest(heelPositions);
    var highestToe = Highest(toePositions);
    return Math.Max(highestHeel.y, highestToe.y);
  }

  static float calculateβ(Vector3 motionAxis) {
    return motionAxis.magnitude;
  }

  public int StanceTimeIndex(
  Vector3[] heelPositions,
  Vector3[] toePositions,
  Vector3[] footPositions,
  Vector3 averageGroundPosition,
  Vector3 motionAxis) {
    var α = calculateα(heelPositions, toePositions) / GroundedImportance;
    var β = calculateβ(motionAxis) / CenteredImportance;
    var index = 0;
    var lowestCost = float.MaxValue;
    for (var i = 0; i < footPositions.Length; i++) {
      var cost = Cost(
        heelPositions[i].y,
        toePositions[i].y,
        footPositions[i],
        averageGroundPosition,
        motionAxis,
        α,
        β);
      if (cost <= lowestCost) {
        lowestCost = cost;
        index = i;
      }
    }
    return index;
  }
  // END Heuristic analysis

  public static Vector3 FootDirection(
  Vector3 toePosition,
  Vector3 heelPosition,
  float footLength) {
    var d = toePosition - heelPosition;
    d.y = 0;
    return d.normalized * footLength;
  }

  public static float Balance(
  float heelHeight,
  float toeHeight,
  float footLength,
  float α = 40) {
    const float π = Mathf.PI;
    return Mathf.Atan((heelHeight-toeHeight)*α/footLength)/π + .5f;
  }

  public static Vector3 FootBase(
  Vector3 heelPosition,
  Vector3 toePosition,
  Vector3 footDirection,
  float balance) {
    return heelPosition*(1-balance) + (toePosition-footDirection)*balance;
  }

  [SerializeField] GameObject ModelPrefab;
  [SerializeField] int FrameRate = 30;
  [SerializeField] float GroundedImportance = 1;
  [SerializeField] float CenteredImportance = 1;

  public AnimationClip AnimationClip;
  public int FrameCount;
  public Cycle Cycle;
  public Foot LeftFoot;
  public Foot RightFoot;

  public Vector3[] LeftHeelPositions;
  public Vector3[] LeftToePositions;
  public Vector3[] LeftFootPositions;
  public Vector3[] LeftFootGroundPositions;
  public Vector3[] LeftFootDirections;
  public Vector3[] LeftFootBases;
  public float[] LeftFootBalances;
  public Vector3 LeftStrideVector;
  public Vector3 LeftCycleDirectionEstimate;
  public float LeftCycleDistanceEstimate;

  public Vector3[] RightHeelPositions;
  public Vector3[] RightToePositions;
  public Vector3[] RightFootPositions;
  public Vector3[] RightFootGroundPositions;
  public Vector3[] RightFootDirections;
  public Vector3[] RightFootBases;
  public float[] RightFootBalances;
  public Vector3 RightStrideVector;
  public Vector3 RightCycleDirectionEstimate;
  public float RightCycleDistanceEstimate;

  public float LeftFootLength;
  public Vector3 LeftAverage;
  public Vector3 LeftExtent1;
  public Vector3 LeftExtent2;
  public Vector3 LeftAxis;

  public float RightFootLength;
  public Vector3 RightAverage;
  public Vector3 RightExtent1;
  public Vector3 RightExtent2;
  public Vector3 RightAxis;

  public Vector3 leftHeelLocalPosition;
  public Vector3 leftToeLocalPosition;
  public Vector3 rightHeelLocalPosition;
  public Vector3 rightToeLocalPosition;

  [ContextMenu("Sample")]
  public void Sample() {
    var graph = PlayableGraph.Create("FootBaseSampler");
    var model = (GameObject)PrefabUtility.InstantiatePrefab(ModelPrefab);
    try {
      var animator = model.GetComponent<Animator>();
      animator.applyRootMotion = false;
      animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
      animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
      var leftHeel = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
      var leftToe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
      var rightHeel = animator.GetBoneTransform(HumanBodyBones.RightFoot);
      var rightToe = animator.GetBoneTransform(HumanBodyBones.RightToes);
      FrameCount = Mathf.RoundToInt(AnimationClip.length * FrameRate);
      LeftHeelPositions = new Vector3[FrameCount];
      LeftToePositions = new Vector3[FrameCount];
      LeftFootPositions = new Vector3[FrameCount];
      LeftFootGroundPositions = new Vector3[FrameCount];
      LeftFootDirections = new Vector3[FrameCount];
      LeftFootBases = new Vector3[FrameCount];
      LeftFootBalances = new float[FrameCount];
      RightHeelPositions = new Vector3[FrameCount];
      RightToePositions = new Vector3[FrameCount];
      RightFootPositions = new Vector3[FrameCount];
      RightFootGroundPositions = new Vector3[FrameCount];
      RightFootDirections = new Vector3[FrameCount];
      RightFootBases = new Vector3[FrameCount];
      RightFootBalances = new float[FrameCount];

      // Assume heel straight down from the ankle to the ground
      // Assume ball straight down from toe to the ground
      var leftHeelWorldPosition = leftHeel.position + leftHeel.position.y*Vector3.down;
      leftHeelLocalPosition = leftHeel.InverseTransformPoint(leftHeelWorldPosition);
      var leftToeWorldPosition = leftToe.position + leftToe.position.y*Vector3.down;
      leftToeLocalPosition = leftToe.InverseTransformPoint(leftToeWorldPosition);
      var rightHeelWorldPosition = rightHeel.position + rightHeel.position.y*Vector3.down;
      rightHeelLocalPosition = rightHeel.InverseTransformPoint(rightHeelWorldPosition);
      var rightToeWorldPosition = rightToe.position + rightToe.position.y*Vector3.down;
      rightToeLocalPosition = rightToe.InverseTransformPoint(rightToeWorldPosition);

      LeftFootLength = Vector3.Distance(leftHeelWorldPosition, leftToeWorldPosition);
      RightFootLength = Vector3.Distance(rightHeelWorldPosition, rightToeWorldPosition);

      var playable = AnimationClipPlayable.Create(graph, AnimationClip);
      var output = AnimationPlayableOutput.Create(graph, "Output", animator);
      playable.SetDuration(AnimationClip.length);
      playable.Play();
      output.SetSourcePlayable(playable);
      graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
      graph.Play();

      // sample animation clip recording heel, toe, and foot positions
      for (var i = 0; i < FrameCount; i++) {
        var time = Mathf.Lerp(0, AnimationClip.length, Mathf.InverseLerp(0, FrameCount-1, i));
        playable.SetTime(time);
        graph.Evaluate();
        LeftHeelPositions[i] = leftHeel.TransformPoint(leftHeelLocalPosition);
        LeftToePositions[i] = leftToe.TransformPoint(leftToeLocalPosition);
        LeftFootPositions[i] = (leftToe.position + leftHeel.position) / 2f;
        LeftFootDirections[i] = FootDirection(LeftToePositions[i], LeftHeelPositions[i], LeftFootLength);
        LeftFootGroundPositions[i] = LeftFootPositions[i];
        LeftFootGroundPositions[i].y = 0;
        LeftFootBalances[i] = Balance(LeftHeelPositions[i].y, LeftToePositions[i].y, LeftFootLength);
        LeftFootBases[i] = FootBase(LeftHeelPositions[i], LeftToePositions[i], LeftFootDirections[i], LeftFootBalances[i]);

        RightHeelPositions[i] = rightHeel.TransformPoint(rightHeelLocalPosition);
        RightToePositions[i] = rightToe.TransformPoint(rightToeLocalPosition);
        RightFootPositions[i] = (rightToe.position + rightHeel.position) / 2f;
        RightFootDirections[i] = FootDirection(RightToePositions[i], RightHeelPositions[i], RightFootLength);
        RightFootGroundPositions[i] = RightFootPositions[i];
        RightFootGroundPositions[i].y = 0;
        RightFootBalances[i] = Balance(RightHeelPositions[i].y, RightToePositions[i].y, RightFootLength);
        RightFootBases[i] = FootBase(RightHeelPositions[i], RightToePositions[i], RightFootDirections[i], RightFootBalances[i]);
      }

      LeftAverage = Average(LeftFootGroundPositions);
      LeftExtent1 = FurthestFrom(LeftFootGroundPositions, LeftAverage);
      LeftExtent2 = FurthestFrom(LeftFootGroundPositions, LeftExtent1);
      LeftAxis = LeftExtent1 - LeftExtent2;
      // Analysis approach to heuristically find StanceTime
      // LeftStanceTimeIndex = StanceTimeIndex(
      //   LeftHeelPositions,
      //   LeftToePositions,
      //   LeftFootPositions,
      //   LeftAverage,
      //   LeftAxis);

      RightAverage = Average(RightFootGroundPositions);
      RightExtent1 = FurthestFrom(RightFootGroundPositions, RightAverage);
      RightExtent2 = FurthestFrom(RightFootGroundPositions, RightExtent1);
      RightAxis = RightExtent1 - RightExtent2;
      // Analysis approach to heuristically find StanceTime
      // RightStanceTimeIndex = StanceTimeIndex(
      //   RightHeelPositions,
      //   RightToePositions,
      //   RightFootPositions,
      //   RightAverage,
      //   RightAxis);

      LeftFoot.Stride.StancePosition = LeftFootBases[LeftFoot.StrideFrames.Stance];
      LeftFoot.Stride.StanceRotation = LeftFootDirections[LeftFoot.StrideFrames.Stance];
      LeftFoot.Stride.StanceCycleTime = (float)LeftFoot.StrideFrames.Stance/FrameCount;
      LeftFoot.Stride.FootStrikeStrideTime = StrideNormalizedTime(LeftFoot.StrideFrames.Stance, LeftFoot.StrideFrames.FootStrike, FrameCount);
      LeftFoot.Stride.FootLandStrideTime = StrideNormalizedTime(LeftFoot.StrideFrames.Stance, LeftFoot.StrideFrames.FootLand, FrameCount);
      LeftFoot.Stride.FootLiftStrideTime = StrideNormalizedTime(LeftFoot.StrideFrames.Stance, LeftFoot.StrideFrames.FootLift, FrameCount);
      LeftFoot.Stride.FootOffStrideTime = StrideNormalizedTime(LeftFoot.StrideFrames.Stance, LeftFoot.StrideFrames.FootOff, FrameCount);
      LeftStrideVector = LeftFootBases[LeftFoot.StrideFrames.FootOff]-LeftFootBases[LeftFoot.StrideFrames.FootStrike];
      LeftCycleDirectionEstimate = -LeftStrideVector.normalized;
      LeftCycleDistanceEstimate = LeftStrideVector.magnitude / Cyclic(LeftFoot.Stride.FootOffStrideTime-LeftFoot.Stride.FootStrikeStrideTime);

      RightFoot.Stride.StancePosition = RightFootBases[RightFoot.StrideFrames.Stance];
      RightFoot.Stride.StanceRotation = RightFootDirections[RightFoot.StrideFrames.Stance];
      RightFoot.Stride.StanceCycleTime = (float)RightFoot.StrideFrames.Stance/FrameCount;
      RightFoot.Stride.FootStrikeStrideTime = StrideNormalizedTime(RightFoot.StrideFrames.Stance, RightFoot.StrideFrames.FootStrike, FrameCount);
      RightFoot.Stride.FootLandStrideTime = StrideNormalizedTime(RightFoot.StrideFrames.Stance, RightFoot.StrideFrames.FootLand, FrameCount);
      RightFoot.Stride.FootLiftStrideTime = StrideNormalizedTime(RightFoot.StrideFrames.Stance, RightFoot.StrideFrames.FootLift, FrameCount);
      RightFoot.Stride.FootOffStrideTime = StrideNormalizedTime(RightFoot.StrideFrames.Stance, RightFoot.StrideFrames.FootOff, FrameCount);
      RightStrideVector = RightFootBases[RightFoot.StrideFrames.FootOff]-RightFootBases[RightFoot.StrideFrames.FootStrike];
      RightCycleDirectionEstimate = -RightStrideVector.normalized;
      RightCycleDistanceEstimate = RightStrideVector.magnitude / Cyclic(RightFoot.Stride.FootOffStrideTime-RightFoot.Stride.FootStrikeStrideTime);

      Cycle.Duration = AnimationClip.length;
      Cycle.Distance = (LeftCycleDistanceEstimate + RightCycleDistanceEstimate) / 2;
      Cycle.Direction = (LeftCycleDirectionEstimate + RightCycleDirectionEstimate) / 2;
      Cycle.Speed = Cycle.Distance / Cycle.Duration;

      var Q = Quaternion.FromToRotation(Cycle.Direction, Vector3.forward);
      LeftFoot.FootBase = new FootBase[FrameCount];
      for (var i = 0; i < FrameCount; i++) {
        var cycleTime = (float)i/FrameCount;
        var fbc = LeftFootBases[i];
        var fbw = fbc + cycleTime * Cycle.Distance * Cycle.Direction;
        fbw = Q * fbw;
        fbw.z /= Cycle.Distance;
        LeftFoot.FootBase[i].TranslationOffset = fbw;
      }
      RightFoot.FootBase = new FootBase[FrameCount];
      for (var i = 0; i < FrameCount; i++) {
        var cycleTime = (float)i/FrameCount;
        var fbc = RightFootBases[i];
        var fbw = fbc + cycleTime * Cycle.Distance * Cycle.Direction;
        fbw = Q * fbw;
        fbw.z /= Cycle.Distance;
        RightFoot.FootBase[i].TranslationOffset = fbw;
      }
    } finally {
      graph.Destroy();
      DestroyImmediate(model);
    }
  }
}