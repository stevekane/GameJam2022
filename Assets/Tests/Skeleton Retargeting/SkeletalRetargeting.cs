using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public struct OscillateJob: IAnimationJob {
  public float Time;
  public ReadWriteTransformHandle Handle;
  public OscillateJob(float time, ReadWriteTransformHandle handle) {
    Time = time;
    Handle = handle;
  }
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    Handle.SetLocalRotation(stream, quaternion.RotateZ(math.PI * math.sin(Time)));
  }
}

public class SkeletalRetargeting : MonoBehaviour {
  public Animator Animator;
  public Transform[] SkeletonTransforms;
  public Transform[] RigTransforms;

  PlayableGraph Graph;
  NativeArray<ReadWriteTransformHandle> SkeletonBones;
  NativeArray<ReadWriteTransformHandle> RigBones;
  AnimationScriptPlayable OscillatePlayable;
  AnimationScriptPlayable RetargetPlayable;
  AnimationPlayableOutput AnimationOutput;

  void Start() {
    Graph = PlayableGraph.Create("Skeleton Retargeting");
    SkeletonBones = new(SkeletonTransforms.Length, Allocator.Persistent);
    RigBones = new(RigTransforms.Length, Allocator.Persistent);
    SkeletonTransforms.ForEach((t,i) => SkeletonBones[i] = ReadWriteTransformHandle.Bind(Animator, t));
    RigTransforms.ForEach((t,i) => RigBones[i] = ReadWriteTransformHandle.Bind(Animator, t));
    OscillatePlayable = AnimationScriptPlayable.Create<OscillateJob>(Graph, new(Time.time, SkeletonBones[0]));
    RetargetPlayable = AnimationScriptPlayable.Create<RetargetingJob>(Graph, new(SkeletonBones, RigBones));
    AnimationOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
    RetargetPlayable.AddInput(OscillatePlayable, 0, 1);
    AnimationOutput.SetSourcePlayable(RetargetPlayable);
    Graph.Play();
  }

  void Update() {
    OscillatePlayable.SetJobData<OscillateJob>(new(Time.time, SkeletonBones[0]));
  }

  void OnDestroy() {
    Graph.Destroy();
    SkeletonBones.Dispose();
    RigBones.Dispose();
  }
}