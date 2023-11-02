using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// An list of the most common `humanName` values in avatars.
public enum AvatarBone {
  Hips,
  LeftUpperLeg,
  RightUpperLeg,
  LeftLowerLeg,
  RightLowerLeg,
  LeftFoot,
  RightFoot,
  Spine,
  Chest,
  Neck,
  Head,
  LeftShoulder,
  RightShoulder,
  LeftHand,
  RightHand,
  LeftLowerArm,
  RightLowerArm,
  UpperChest,
  // there are others but who cares
}

public class AvatarAttacher : MonoBehaviour {
  [SerializeField] Animator Animator;
  Dictionary<AvatarBone, Transform> BoneToTransform = new();

  public Transform GetBoneTransform(AvatarBone bone) => BoneToTransform[bone];
  public static Transform FindBoneTransform(Animator animator, AvatarBone bone) {
    var boneName = bone.ToString();
    var hb = animator.avatar.humanDescription.human.FirstOrDefault(hb => hb.humanName == boneName);
    if (hb.humanName == boneName)
      return animator.transform.FindDescendant(hb.boneName);
    return null;
  }

  void Awake() {
    var boneEnums = (AvatarBone[])Enum.GetValues(typeof(AvatarBone));
    var boneNames = Enum.GetNames(typeof(AvatarBone));
    foreach (var humanBone in Animator.avatar.humanDescription.human) {
      if (Array.FindIndex(boneNames, n => n == humanBone.humanName) is var idx && idx >= 0)
        BoneToTransform.Add(boneEnums[idx], Animator.transform.FindDescendant(humanBone.boneName));
    }

    GetComponentsInChildren<AvatarAttachment>().ForEach(TryReparent);
    GetComponentsInChildren<AvatarTransform>().ForEach(TrySetTransformReference);
  }

  void TryReparent(AvatarAttachment attachment) {
    Transform foundTransform = GetBoneTransform(attachment.Bone);
    if (foundTransform) {
      attachment.transform.SetParent(foundTransform, true);
    }
  }

  void TrySetTransformReference(AvatarTransform avatarTransform) {
    Transform foundTransform = GetBoneTransform(avatarTransform.Bone);
    if (foundTransform) {
      avatarTransform.Transform = foundTransform;
    }
  }

  #if UNITY_EDITOR
  [ContextMenu("Print Bone Mapping")]
  void PrintMap() {
    foreach (var humanBone in Animator.avatar.humanDescription.human) {
      Debug.Log($"{name}'s avatar {Animator.avatar.name} maps {humanBone.humanName} to {humanBone.boneName}");
    }
  }
  #endif
}