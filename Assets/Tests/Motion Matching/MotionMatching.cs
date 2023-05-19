using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

/*
Construct playable graph.

+ Loop the running animation.
+ Create mixer out of the looping running cycles.
+ Add an attack animation.
+ Read data from the attack animation stream.
+ Mix attack animation with the looping running.
+ Use data from attack animation stream to blend between running cycles.
+ Blend result of blended running cycles with attack.
*/

public struct SampleJob : IAnimationJob {
  public float Angle;
  public ReadOnlyTransformHandle RootHandle;
  public ReadOnlyTransformHandle TargetHandle;
  public SampleJob(ReadOnlyTransformHandle rootHandle, ReadOnlyTransformHandle targetHandle) {
    RootHandle = rootHandle;
    TargetHandle = targetHandle;
    Angle = 0f;
  }
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    var rootRotation = RootHandle.GetRotation(stream.GetInputStream(0));
    var rotation = TargetHandle.GetRotation(stream.GetInputStream(0));
    var localRotation = math.inverse(rootRotation) * rotation;
    var alongHips = math.forward(localRotation);
    var alongHipsXZ = new float3(alongHips.x, 0, alongHips.z);
    Angle = Vector3.SignedAngle(alongHipsXZ, new float3(0, 0, 1), new float3(0, 1, 0));
  }
}

[Serializable]
public struct MixerJobData {
  public NativeArray<ReadWriteTransformHandle> BoneHandles;
  public NativeArray<int> BoneParents;
  public NativeArray<NativeArray<bool>> BoneActivesPerLayer;
  public bool ReverseBaseLayerRotation;
  public bool MeshSpaceRotations;

  AvatarMaskBodyPart[] BoneBodyParts;  // Main thread only.
  public void Init(Animator animator) {
    var transforms = animator.avatarRoot.GetComponentsInChildren<Transform>();
    BoneHandles = new(transforms.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    BoneParents = new(transforms.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    for (int i = 0; i < transforms.Length; i++) {
      BoneHandles[i] = ReadWriteTransformHandle.Bind(animator, transforms[i]);
      BoneParents[i] = Array.FindIndex(transforms, t => t == transforms[i].parent);
    }
    BoneActivesPerLayer = new(0, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

    var self = this;
    BoneBodyParts = transforms.Select(t => self.GetAvatarMaskBodyPart(animator.avatarRoot, animator.avatar, t)).ToArray();
  }

  public void Dispose() {
    BoneHandles.Dispose();
    BoneParents.Dispose();
    BoneActivesPerLayer.Dispose();
  }

  // Adds a new layer to our BoneMaskPerLayer cache, and turns its bones on/off depending on whether they
  // are enabled by `mask` - defaults to ON if `mask` is null.
  public void SetLayerMaskFromAvatarMask(int layer, AvatarMask mask) {
    var old = BoneActivesPerLayer;
    BoneActivesPerLayer = new(layer+1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    NativeArray<NativeArray<bool>>.Copy(old, BoneActivesPerLayer, layer);
    var boneActives = BoneActivesPerLayer[layer] = new(BoneBodyParts.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    for (int bone = 0; bone < BoneBodyParts.Length; bone++)
      boneActives[bone] = mask ? mask.GetHumanoidBodyPartActive(BoneBodyParts[bone]) : true;
    old.Dispose();
  }

  static Dictionary<string, AvatarMaskBodyPart> HumanNameToBodyPart = new() {
    {"Root", AvatarMaskBodyPart.Root },
    {"Spine", AvatarMaskBodyPart.Body },
    {"Head", AvatarMaskBodyPart.Head },
    {"LeftUpperLeg", AvatarMaskBodyPart.LeftLeg },
    {"RightUpperLeg", AvatarMaskBodyPart.RightLeg },
    {"LeftShoulder", AvatarMaskBodyPart.LeftArm },
    {"RightShoulder", AvatarMaskBodyPart.RightArm },
    //{"LeftHand", AvatarMaskBodyPart.LeftFingers }, // fuck fingers
    //{"RightHand", AvatarMaskBodyPart.RightFingers },
    {"LeftFoot", AvatarMaskBodyPart.LeftFootIK },
    {"RightFoot", AvatarMaskBodyPart.RightFootIK },
    {"LeftHand", AvatarMaskBodyPart.LeftHandIK },
    {"RightHand", AvatarMaskBodyPart.RightHandIK },
  };
  // Returns the AvatarMaskBodyPart for the region of the avatar tree given by `t`, which must be a descendent
  // of avatarRoot.
  AvatarMaskBodyPart GetAvatarMaskBodyPart(Transform avatarRoot, Avatar avatar, Transform t) {
    if (avatar == null || t == avatarRoot)
      return AvatarMaskBodyPart.Root;
    var hb = avatar.humanDescription.human.FirstOrDefault(hb => hb.boneName == t.name);
    if (hb.boneName == t.name && HumanNameToBodyPart.TryGetValue(hb.humanName, out var bodyPart))
      return bodyPart;
    return GetAvatarMaskBodyPart(avatarRoot, avatar, t.parent);
  }
}

public struct MixerJob : IAnimationJob {
  public MixerJobData Data;
  public float Angle;
  public MixerJob(MixerJobData data) {
    Data = data;
    Angle = 0f;
  }
  public void ProcessRootMotion(AnimationStream stream) {
    // I don't know what this shit is used for.
    var baseStream = stream.GetInputStream(0);
    stream.velocity = baseStream.velocity;
    stream.angularVelocity = baseStream.angularVelocity;
  }
  public void ProcessAnimation(AnimationStream stream) {
    Angle = 0f;
    var baseStream = stream.GetInputStream(0);
    for (var i = 0; i < Data.BoneHandles.Length; i++) {
      var handle = Data.BoneHandles[i];
      int layer = FindInputStreamForBone(stream, i);
      var inStream = stream.GetInputStream(layer);
      var inWeight = stream.GetInputWeight(layer);
      handle.SetLocalPosition(stream, Blend(handle.GetLocalPosition(baseStream), handle.GetLocalPosition(inStream), inWeight));
      handle.SetLocalScale(stream, Blend(handle.GetLocalScale(baseStream), handle.GetLocalScale(inStream), inWeight));
      var boneIsTopmostActive = Data.BoneActivesPerLayer[layer][i] && Data.BoneParents[i] >= 0 && !Data.BoneActivesPerLayer[layer][Data.BoneParents[i]];
      if (boneIsTopmostActive) {
        // TODO HACK: untangle this hip angle sampling and make a better mixer.
        var parentHandle = Data.BoneHandles[Data.BoneParents[i]];
        ReadHipAngle(parentHandle, inStream);

        // If our parent bone is disabled in this layer, do some special shit to try to preserve the animation's intent for this bone.
        // First, use a "mesh-space" rotation - figure out the total rotation that WOULD be imparted on this bone and apply it locally.
        Quaternion rotation = Data.MeshSpaceRotations ?
          Quaternion.Inverse(RootRotation(inStream)) * handle.GetRotation(inStream) :
          handle.GetLocalRotation(inStream);
        if (Data.ReverseBaseLayerRotation) {
          // Next, reverse the rotation that our base layer applies to our parent bone.
          var baseParentRotation = Quaternion.Inverse(RootRotation(baseStream)) * parentHandle.GetRotation(baseStream);
          rotation = Quaternion.Inverse(baseParentRotation) * rotation;
        }
        handle.SetLocalRotation(stream, Blend(handle.GetLocalRotation(baseStream), rotation, inWeight));
      } else {
        handle.SetLocalRotation(stream, Blend(handle.GetLocalRotation(baseStream), handle.GetLocalRotation(inStream), inWeight));
      }
    }
  }
  public void ReadHipAngle(ReadWriteTransformHandle handle, AnimationStream stream) {
    var rootRotation = RootRotation(stream);
    var rotation = handle.GetRotation(stream);
    //var localRotation = TargetHandle.GetLocalRotation(stream);
    var localRotation = Quaternion.Inverse(rootRotation) * rotation;
    var alongHips = localRotation * Vector3.forward;
    Angle = Vector3.SignedAngle(alongHips.XZ(), Vector3.forward, Vector3.up);
  }
  Vector3 Blend(Vector3 a, Vector3 b, float weight) => Vector3.Lerp(a, b, weight);
  Quaternion Blend(Quaternion a, Quaternion b, float weight) => Quaternion.Slerp(a, b, weight);
  Quaternion RootRotation(AnimationStream stream) => Data.BoneHandles[0].GetRotation(stream);
  int FindInputStreamForBone(AnimationStream stream, int boneIdx) {
    for (var layer = stream.inputStreamCount-1; layer >= 0; layer--) {
      if (Data.BoneActivesPerLayer[layer][boneIdx] && stream.GetInputStream(layer).isValid)
        return layer;
    }
    return 0;  // default to top layer
  }
}

public class MotionMatching : MonoBehaviour {
  public SkinnedMeshRenderer SkinnedMeshRenderer;
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
    BlendTree.Set(TorsoTwist);
    Debug.LogWarning("Start of MotionMatching no longer sets nodes on blendtree because API changed.");
    // BlendTree.SetNodes(LowerBodyNodes);
    UpperBodyPlayable = AnimationClipPlayable.Create(Graph, UpperBodyClip);
    Output = AnimationPlayableOutput.Create(Graph, "Motion Matching", Animator);

    // BEGIN UPPER BODY SAMPLER
    Sampler = AnimationScriptPlayable.Create(Graph, new SampleJob(
      ReadOnlyTransformHandle.Bind(Animator, Animator.avatarRoot),
      ReadOnlyTransformHandle.Bind(Animator, HipTransform)));
    Sampler.SetProcessInputs(true);

    // BEGIN MESH SPACE MIXER
    MixerJobData.Init(Animator);
    MixerJobData.SetLayerMaskFromAvatarMask(0, LowerBodyMask);
    MixerJobData.SetLayerMaskFromAvatarMask(1, UpperBodyMask);
    JobMixer = AnimationScriptPlayable.Create(Graph, new MixerJob() {
      Data = MixerJobData
    });
    JobMixer.SetProcessInputs(false);
    JobMixer.SetInputCount(2);
    JobMixer.SetInputWeight(1, 1f);

    Sampler.AddInput(UpperBodyPlayable, 0, 1f);
    Graph.Connect(BlendTreePlayable, 0, JobMixer, 0);
    Graph.Connect(Sampler, 0, JobMixer, 1);
    Output.SetSourcePlayable(JobMixer);
    // END MESH SPACE MIXER

    Graph.Play();

    // TODO: HACKY API TESTING
    // var mask = new AvatarMask();
    var mask = UpperBodyMask;
    for (var i = 0; i < SkinnedMeshRenderer.bones.Length; i++) {
      var bone = SkinnedMeshRenderer.bones[i];
      var boneIndex = bone;
      if (!mask.GetTransformActive(i))
        continue;
      Debug.Log($"{bone.name} is active");
    }
    // END HACKY API TESTING

  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void Update() {
    var samplerData = Sampler.GetJobData<SampleJob>();
    TorsoTwist = samplerData.Angle;
    BlendTree.Set(samplerData.Angle);
    BlendTree.BlendCurve = BlendCurve;
  }
}