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
  UpperChest,
  // there are others but who cares
}

public class AvatarAttacher: MonoBehaviour {
  [SerializeField] Animator Animator;
  Dictionary<AvatarBone, Transform> BoneToTransform = new();

  public Transform GetBoneTransform(AvatarBone bone) => BoneToTransform[bone];

  void Awake() {
    GetComponentsInChildren<AvatarAttachment>().ForEach(TryReparent);
    GetComponentsInChildren<AvatarTransform>().ForEach(TrySetTransformReference);

    var boneEnums = (AvatarBone[])Enum.GetValues(typeof(AvatarBone));
    var boneNames = Enum.GetNames(typeof(AvatarBone));
    foreach (var humanBone in Animator.avatar.humanDescription.human) {
      if (Array.FindIndex(boneNames, n => n == humanBone.humanName) is var idx && idx >= 0)
        BoneToTransform.Add(boneEnums[idx], Animator.transform.FindDescendant(humanBone.boneName));
    }
  }

  void TryReparent(AvatarAttachment attachment) {
    HumanBone? targetHumanBone = Animator.avatar.humanDescription.human.FirstOrDefault(hb => hb.humanName == attachment.HumanName);
    if (targetHumanBone.HasValue) {
      Transform foundTransform = Animator.transform.FindDescendant(targetHumanBone.Value.boneName);
      if (foundTransform) {
        attachment.transform.SetParent(foundTransform, true);
      }
    }
  }

  void TrySetTransformReference(AvatarTransform avatarTransform) {
    HumanBone? targetHumanBone = Animator.avatar.humanDescription.human.FirstOrDefault(hb => hb.humanName == avatarTransform.HumanName);
    if (targetHumanBone.HasValue) {
      avatarTransform.Transform = Animator.transform.FindDescendant(targetHumanBone.Value.boneName);
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