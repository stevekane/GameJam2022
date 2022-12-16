using System.Linq;
using UnityEngine;

public class AvatarAttacher: MonoBehaviour {
  [SerializeField] Animator Animator;

  void Awake() {
    GetComponentsInChildren<AvatarAttachment>().ForEach(TryReparent);
    GetComponentsInChildren<AvatarTransform>().ForEach(TrySetTransformReference);
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