using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Mathematics;

/*
Construct playable graph.

+ Loop the running animation.
+ Create mixer out of the looping running cycles.
Add an attack animation.
Read data from the attack animation stream.
Mix attack animation with the looping running.
Use data from attack animation stream to blend between running cycles.
Blend result of blended running cycles with attack.
*/
[Serializable]
public struct BlendTreeNode : IComparable<BlendTreeNode> {
  public AnimationClip Clip;
  public float Value;
  public float YRotation;
  public int CompareTo(BlendTreeNode b) => Value.CompareTo(b.Value);
}

public class BlendTreeBehaviour : PlayableBehaviour {
  public AnimationCurve BlendCurve;
  public Transform Transform;
  public float CycleSpeed = 1;
  public float Value = 0;

  Playable Playable;
  AnimationMixerPlayable Mixer;
  AnimationClipPlayable[] ClipPlayables;
  BlendTreeNode[] Nodes;

  public override void OnPlayableCreate(Playable playable) {
    Mixer = AnimationMixerPlayable.Create(playable.GetGraph(), 0);
    Playable = playable;
    Playable.ConnectInput(0, Mixer, 0);
  }

  public override void OnPlayableDestroy(Playable playable) {
    Mixer.Destroy();
    foreach (var clipPlayable in ClipPlayables) {
      clipPlayable.Destroy();
    }
  }

  public override void PrepareFrame(Playable playable, FrameData info) {
    for (var i = 0; i < Nodes.Length; i++) {
      var weight = Weight(i, Value, Nodes);
      Mixer.SetInputWeight(i, weight);
      ClipPlayables[i].SetSpeed(Nodes[i].Clip.length / CycleSpeed);
    }
  }

  public void SetNodes(BlendTreeNode[] nodes) {
    Nodes = nodes.SortedCopy();
    ClipPlayables = new AnimationClipPlayable[Nodes.Length];
    Mixer.SetInputCount(Nodes.Length);
    for (var i = 0; i < Nodes.Length; i++) {
      var node = Nodes[i];
      var playable = AnimationClipPlayable.Create(Playable.GetGraph(), node.Clip);
      playable.SetSpeed(node.Clip.length / CycleSpeed);
      Mixer.ConnectInput(i, playable, 0, 0);
      ClipPlayables[i] = playable;
    }
  }

  float Weight(int index, float value, BlendTreeNode[] nodes) {
    var minIndex = 0;
    var maxIndex = nodes.Length-1;
    if (index < minIndex || index > maxIndex) {
      return 0;
    } else if (maxIndex == 0) {
      return 1;
    } else if (index == minIndex && value < nodes[minIndex].Value) {
      return 1;
    } else if (index == maxIndex && value > nodes[maxIndex].Value) {
      return 1;
    } else if (index > minIndex && value >= nodes[index-1].Value && value <= nodes[index].Value) {
      return BlendCurve.Evaluate(Mathf.InverseLerp(nodes[index-1].Value, nodes[index].Value, value));
    } else if (index < maxIndex && value >= nodes[index].Value && value <= nodes[index+1].Value) {
      return 1-BlendCurve.Evaluate(Mathf.InverseLerp(nodes[index].Value, nodes[index+1].Value, value));
    } else {
      return 0;
    }
  }
}

public struct SampleJob : IAnimationJob {
  public quaternion Rotation;
  public ReadOnlyTransformHandle Handle;
  public SampleJob(ReadOnlyTransformHandle handle) {
    Rotation = quaternion.identity;
    Handle = handle;
  }
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    Rotation = Handle.GetRotation(stream.GetInputStream(0));
  }
}

public class MotionMatching : MonoBehaviour {
  public Animator Animator;
  public Transform HipTransform;
  public AvatarMask LowerBodyMask;
  public AvatarMask UpperBodyMask;
  public AnimationClip UpperBodyClip;
  public BlendTreeNode[] LowerBodyNodes;
  public AnimationCurve BlendCurve;
  [Range(.1f, 10)]
  public float CycleSpeed = 1;
  [Range(.1f, 10)]
  public float TwistPeriod = 1;
  public float MinTwist = -90;
  public float MaxTwist = 90;
  [Range(-90, 90)]
  public float TorsoTwist;
  public PlayableGraph Graph;
  public AnimationPlayableOutput Output;
  public AnimationClipPlayable UpperBodyPlayable;
  public AnimationLayerMixerPlayable FinalMixer;
  public ScriptPlayable<BlendTreeBehaviour> BlendTreePlayable;
  public MixerJobData MixerJobData = new();
  public AnimationScriptPlayable JobMixer;
  public AnimationScriptPlayable Sampler;
  public BlendTreeBehaviour BlendTree;

  void Start() {
    Graph = PlayableGraph.Create("Motion Matching");
    BlendTreePlayable = ScriptPlayable<BlendTreeBehaviour>.Create(Graph, 1);
    BlendTree = BlendTreePlayable.GetBehaviour();
    BlendTree.BlendCurve = BlendCurve;
    BlendTree.CycleSpeed = CycleSpeed;
    BlendTree.Value = TorsoTwist;
    BlendTree.Transform = transform;
    BlendTree.SetNodes(LowerBodyNodes);
    UpperBodyPlayable = AnimationClipPlayable.Create(Graph, UpperBodyClip);
    Output = AnimationPlayableOutput.Create(Graph, "Motion Matching", Animator);

    // BEGIN UPPER BODY SAMPLER
    Sampler = AnimationScriptPlayable.Create(Graph, new SampleJob {
      Handle = ReadOnlyTransformHandle.Bind(Animator, HipTransform)
    });
    Sampler.SetProcessInputs(true);
    Sampler.AddInput(UpperBodyPlayable, 0, 1);

    // BEGIN MESH SPACE MIXER
    MixerJobData.Init(Animator);
    MixerJobData.SetLayerMaskFromAvatarMask(0, LowerBodyMask);
    MixerJobData.SetLayerMaskFromAvatarMask(1, UpperBodyMask);
    JobMixer = AnimationScriptPlayable.Create(Graph, new MixerJob() {
      Data = MixerJobData
    });
    JobMixer.SetProcessInputs(false);
    JobMixer.SetInputCount(2);
    Graph.Connect(BlendTreePlayable, 0, JobMixer, 0);
    Graph.Connect(Sampler, 0, JobMixer, 1);
    Output.SetSourcePlayable(JobMixer);
    // END MESH SPACE MIXER

    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void Update() {
    var samplerData = Sampler.GetJobData<SampleJob>();
    var hipRotation = samplerData.Rotation;
    var alongHips = math.forward(hipRotation);
    var alongHipsXZ = new float3(alongHips.x, 0, alongHips.z);
    var angle = Vector3.SignedAngle(alongHipsXZ, new float3(0,0,1), new float3(0,1,0));

    TorsoTwist = angle;
    BlendTree.Value = angle;
    BlendTree.CycleSpeed = CycleSpeed;
    BlendTree.BlendCurve = BlendCurve;
  }
}