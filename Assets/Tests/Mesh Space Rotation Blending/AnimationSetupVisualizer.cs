using UnityEngine;
using UnityEngine.Animations.Rigging;
using Unity.Collections;

/*
Setup

  SkinnedMeshRenderer being driven by animation stream coming from
  an Animator which has an associated Avatar

  The default stream created for such a setup would come from an
  associated AnimatorController which is instantiated into a
  runtimeAnimatorController instance that is an interface to an
  underlying PlayableGraph.

  We want to first understand how to get a transform handle to
  every single animated bone in our instance of an animated mesh.

  This is done by taking the description of all the humanoid bones
  animated for this character then using the Avatar human map
  to find the path to the associated bone in the skeleton.

  This skeleton path is then used to find the actual bone itself
  and construct a ReadWriteHandle for this bone.
*/
public class AnimationSetupVisualizer : MonoBehaviour {
  [SerializeField] SkinnedMeshRenderer SkinnedMeshRenderer;
  [SerializeField] Animator Animator;
  [SerializeField] AvatarMask AvatarMask;

  NativeArray<ReadWriteTransformHandle> BoneHandles;
  NativeArray<float> Weights;

  void Start() {
    var human = Animator.avatar.humanDescription.human;
    var boneCount = human.Length;
    var rootBone = SkinnedMeshRenderer.rootBone;
    BoneHandles = new NativeArray<ReadWriteTransformHandle>(boneCount, Allocator.Persistent);
    Weights = new NativeArray<float>(boneCount, Allocator.Persistent);
    for (var i = 0; i < boneCount; i++) {
      var boneName = human[i].boneName;
      var bone = rootBone.FindDescendant(boneName);
      BoneHandles[i] = ReadWriteTransformHandle.Bind(Animator, bone);
    }
    for (var i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)  {
      var part = (AvatarMaskBodyPart)i;
      // Debug.Log($"{part} {AvatarMask.GetHumanoidBodyPartActive(part)}");
    }

    // All possible human bones associated with this Animator
    var totalBones = (int)HumanBodyBones.LastBone;
    for (var i = 0; i < totalBones; i++) {
      var humanBone = (HumanBodyBones)i;
      var bone = Animator.GetBoneTransform(humanBone);
      // Debug.Log($"Found {humanBone} {bone}");
    }
  }

  void OnDestroy() {
    BoneHandles.Dispose();
    Weights.Dispose();
  }

  void Update() {
  }
}