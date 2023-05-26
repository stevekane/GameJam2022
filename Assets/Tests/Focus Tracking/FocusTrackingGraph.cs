using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public struct FocusTrackingJob : IAnimationJob {
  public NativeArray<Vector3> CurrentLinkPositions;
  public NativeArray<float> LinkLengths;
  public NativeArray<TransformStreamHandle> Bones;
  public Vector3 GoalPosition;
  public Quaternion GoalRotation;
  public float Tolerance;
  public float Reach;
  public int Iterations;
  public int ReferenceStreamIndex; // 0
  public int AnimationStreamIndex; // 1

  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    AnimationRuntimeUtils.SolveFABRIK(ref CurrentLinkPositions, ref LinkLengths, GoalPosition, Tolerance, Reach, Iterations);
    var referenceStream = stream.GetInputStream(ReferenceStreamIndex);
    var animationStream = stream.GetInputStream(AnimationStreamIndex);
    var referenceRotation = Bones[0].GetRotation(referenceStream);
    var animationLocalRotation = Bones[0].GetLocalRotation(animationStream);
    var refDir = referenceRotation * Vector3.up; // world space reference direction
    var newDir = (CurrentLinkPositions[1] - CurrentLinkPositions[0]).normalized; // world space new direction
    var IKRotation = Quaternion.FromToRotation(refDir, newDir); // TODO: degenerae when vectors co-linear
    Bones[0].SetLocalRotation(stream, animationLocalRotation * IKRotation);

    for (int i = 1; i < Bones.Length; i++) {
      animationLocalRotation = Bones[i].GetLocalRotation(animationStream);
      refDir = (CurrentLinkPositions[i] - CurrentLinkPositions[i - 1]).normalized;
      newDir = (i < Bones.Length - 1)
        ? (CurrentLinkPositions[i + 1] - CurrentLinkPositions[i]).normalized
        : GoalRotation * Vector3.up;
      IKRotation = Quaternion.FromToRotation(refDir, newDir); // TODO: degenerate when vectors co-linear
      Bones[i].SetLocalRotation(stream, animationLocalRotation * IKRotation);
    }

    for (int i = 0; i < Bones.Length-1; i++) {
      Debug.DrawLine(CurrentLinkPositions[i], CurrentLinkPositions[i+1], Color.blue);
    }
  }
}

[Serializable]
public struct FocusTrackingData : IAnimationJobData {
  public Transform Root;
  public Transform Tip;
  public Transform Target;
  public bool IsValid() => true;
  public void SetDefaultValues() {
    Root = null;
    Tip = null;
    Target = null;
  }
}

public class FocusTrackingDataBinder : AnimationJobBinder<FocusTrackingJob, FocusTrackingData> {
  public NativeArray<Vector3> CurrentLinkPositions;
  public NativeArray<float> LinkLengths;
  public NativeArray<TransformStreamHandle> Bones;
  public float Tolerance = 0;
  public float Reach;
  public int Iterations = 25;
  public int ReferenceStreamIndex = 0;
  public int AnimationStreamIndex = 1;

  public override FocusTrackingJob Create(Animator animator, ref FocusTrackingData data, Component component) {
    var bones = ConstraintsUtils.ExtractChain(data.Root, data.Tip);
    CurrentLinkPositions = new NativeArray<Vector3>(bones.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    LinkLengths = new NativeArray<float>(bones.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    Bones = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    for (var i = 0; i < bones.Length; i++) {
      Bones[i] = animator.BindStreamTransform(bones[i]);
    }
    for (var i = 0; i < bones.Length-1; i++) {
      CurrentLinkPositions[i] = bones[i].position;
      LinkLengths[i] = Vector3.Distance(bones[i].position, bones[i+1].position);
      Reach += LinkLengths[i];
    }
    CurrentLinkPositions[LinkLengths.Length-1] = bones[LinkLengths.Length-1].position;
    LinkLengths[LinkLengths.Length-1] = 0;
    return new() {
      CurrentLinkPositions = CurrentLinkPositions,
      LinkLengths = LinkLengths,
      Bones = Bones,
      GoalPosition = data.Target.position,
      GoalRotation = data.Target.rotation,
      Tolerance = Tolerance,
      Reach = Reach,
      Iterations = Iterations,
      ReferenceStreamIndex = ReferenceStreamIndex,
      AnimationStreamIndex = AnimationStreamIndex
    };
  }
  public override void Destroy(FocusTrackingJob job) {
    CurrentLinkPositions.Dispose();
    LinkLengths.Dispose();
    Bones.Dispose();
  }
  public override void Update(FocusTrackingJob job, ref FocusTrackingData data) {
    // THIS IS NOT BEING FIRED. UPDATE THE JOB IN FIXED UPDATE FOR THE GRAPH
    // NOTE!!!!!!!! YOU ARE NOT USING THIS ATM
    job.GoalPosition = data.Target.position;
    job.GoalRotation = data.Target.rotation;
  }
}

public class FocusTrackingGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] FocusTrackingData FocusTrackingData;
  [SerializeField] AnimationClipAsset ReferenceClipAsset;
  [SerializeField] AnimationClipAsset AnimationClipAsset;

  PlayableGraph Graph;
  FocusTrackingJob FocusTrackingJob;
  FocusTrackingDataBinder FocusTrackingDataBinder;
  AnimationClipPlayable AnimationClip;
  AnimationClipPlayable ReferenceClip;
  AnimationScriptPlayable FocusTracking;
  AnimationPlayableOutput Output;

  void Start() {
    Graph = PlayableGraph.Create("Focus Tracking");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    ReferenceClip = (AnimationClipPlayable)ReferenceClipAsset.CreatePlayable(Graph, gameObject);
    AnimationClip = (AnimationClipPlayable)AnimationClipAsset.CreatePlayable(Graph, gameObject);
    FocusTrackingDataBinder = new();
    FocusTrackingJob = FocusTrackingDataBinder.Create(Animator, ref FocusTrackingData, null);
    FocusTracking = AnimationScriptPlayable.Create(Graph, FocusTrackingJob);
    FocusTracking.AddInput(ReferenceClip, 0, 1);
    FocusTracking.AddInput(AnimationClip, 0, 1);
    Output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    Output.SetSourcePlayable(FocusTracking);
  }

  void OnDestroy() {
    FocusTracking.Destroy();
    Graph.Destroy();
  }

  void FixedUpdate() {
    FocusTrackingJob.GoalPosition = FocusTrackingData.Target.position;
    FocusTrackingJob.GoalRotation = FocusTrackingData.Target.rotation;
    FocusTracking.SetJobData(FocusTrackingJob);
    Graph.Evaluate(Time.fixedDeltaTime);
  }
}