using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AnimationGraph/FootBase")]
public class FootBaseAsset : ScriptableObject {
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
    var α = calculateα(heelPositions, toePositions);
    var β = calculateβ(motionAxis);
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
      Debug.Log($"{i} ⇒ {cost}");
      if (cost <= lowestCost) {
        lowestCost = cost;
        index = i;
      }
    }
    return index;
  }

  public static Vector3 FootDirection(Vector3 toePosition, Vector3 heelPosition, float footLength) {
    var d = toePosition - heelPosition;
    d.y = 0;
    return d.normalized * footLength;
  }

  public static float Balance(
  float heelHeight,
  float toeHeight,
  float footLength,
  float α = 80) {
    const float π = Mathf.PI;
    return 1-(Mathf.Atan((heelHeight-toeHeight)*α/footLength)/π + .5f);
  }

  public static Vector3 FootBase(
  Vector3 heelPosition,
  Vector3 toePosition,
  Vector3 footDirection,
  float balance) {
    return heelPosition*balance + (toePosition-footDirection)*(1-balance);
  }

  [SerializeField] GameObject ModelPrefab;
  [SerializeField] int FrameRate = 30;

  public AnimationClip AnimationClip;
  public Vector3[] LeftHeelPositions;
  public Vector3[] LeftToePositions;
  public Vector3[] LeftFootPositions;
  public Vector3[] LeftFootGroundPositions;
  public Vector3[] LeftFootDirections;
  public Vector3[] LeftFootBases;
  public float[] LeftFootBalances;
  public Vector3[] RightHeelPositions;
  public Vector3[] RightToePositions;
  public Vector3[] RightFootPositions;
  public Vector3[] RightFootGroundPositions;
  public Vector3[] RightFootDirections;
  public Vector3[] RightFootBases;
  public float[] RightFootBalances;
  public float LeftFootLength;
  public Vector3 LeftAverage;
  public Vector3 LeftExtent1;
  public Vector3 LeftExtent2;
  public Vector3 LeftAxis;
  public int LeftStanceTimeIndex;
  public float RightFootLength;
  public Vector3 RightAverage;
  public Vector3 RightExtent1;
  public Vector3 RightExtent2;
  public Vector3 RightAxis;
  public int RightStanceTimeIndex;

  [ContextMenu("Sample")]
  public void Sample() {
    var graph = PlayableGraph.Create("FootBaseSampler");
    var model = (GameObject)PrefabUtility.InstantiatePrefab(ModelPrefab);
    try {
      var animator = model.GetComponent<Animator>();
      animator.applyRootMotion = false;
      animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
      animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
      var totalFrames = Mathf.RoundToInt(AnimationClip.length * FrameRate);
      var leftHeel = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
      var leftToe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
      var rightHeel = animator.GetBoneTransform(HumanBodyBones.RightFoot);
      var rightToe = animator.GetBoneTransform(HumanBodyBones.RightToes);
      LeftHeelPositions = new Vector3[totalFrames];
      LeftToePositions = new Vector3[totalFrames];
      LeftFootPositions = new Vector3[totalFrames];
      LeftFootGroundPositions = new Vector3[totalFrames];
      LeftFootDirections = new Vector3[totalFrames];
      LeftFootBases = new Vector3[totalFrames];
      LeftFootBalances = new float[totalFrames];
      RightHeelPositions = new Vector3[totalFrames];
      RightToePositions = new Vector3[totalFrames];
      RightFootPositions = new Vector3[totalFrames];
      RightFootGroundPositions = new Vector3[totalFrames];
      RightFootDirections = new Vector3[totalFrames];
      RightFootBases = new Vector3[totalFrames];
      RightFootBalances = new float[totalFrames];

      // Assume heel straight down from the ankle to the ground
      // Assume ball straight down from toe to the ground
      var leftHeelWorldPosition = leftHeel.position + leftHeel.position.y*Vector3.down;
      var leftHeelLocalPosition = leftHeel.InverseTransformPoint(leftHeelWorldPosition);
      var leftToeWorldPosition = leftToe.position + leftToe.position.y*Vector3.down;
      var leftToeLocalPosition = leftToe.InverseTransformPoint(leftToeWorldPosition);
      var rightHeelWorldPosition = rightHeel.position + rightHeel.position.y*Vector3.down;
      var rightHeelLocalPosition = rightHeel.InverseTransformPoint(rightHeelWorldPosition);
      var rightToeWorldPosition = rightToe.position + rightToe.position.y*Vector3.down;
      var rightToeLocalPosition = rightToe.InverseTransformPoint(rightToeWorldPosition);

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
      for (var i = 0; i < totalFrames; i++) {
        var time = Mathf.Lerp(0, AnimationClip.length, Mathf.InverseLerp(0, totalFrames-1, i));
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
      LeftStanceTimeIndex = StanceTimeIndex(
        LeftHeelPositions,
        LeftToePositions,
        LeftFootPositions,
        LeftAverage,
        LeftAxis);

      RightAverage = Average(RightFootGroundPositions);
      RightExtent1 = FurthestFrom(RightFootGroundPositions, RightAverage);
      RightExtent2 = FurthestFrom(RightFootGroundPositions, RightExtent1);
      RightAxis = RightExtent1 - RightExtent2;
      RightStanceTimeIndex = StanceTimeIndex(
        RightHeelPositions,
        RightToePositions,
        RightFootPositions,
        RightAverage,
        RightAxis);
    } finally {
      graph.Destroy();
      DestroyImmediate(model);
    }
  }
}