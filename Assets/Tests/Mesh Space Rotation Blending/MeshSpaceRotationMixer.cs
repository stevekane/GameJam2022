using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Animations.Rigging;
using Unity.Collections;

/*
Let's create a job that masks out bones from the AnimationStream using an AvatarMask.
*/

public struct AnimationMaskJob : IAnimationJob {
  public AvatarMask Mask;
  public NativeArray<ReadWriteTransformHandle> Handles;
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    var data = Handles[0].GetLocalRotation(stream);
  }
}

public class MeshSpaceRotationMixer: MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] SkinnedMeshRenderer SkinnedMeshRenderer;
  [SerializeField] AnimationClip LowerBodyClip;
  [SerializeField] AnimationClip UpperBodyClip;
  [SerializeField] Avatar Avatar;
  [SerializeField] AvatarMask LowerBodyMask;
  [SerializeField] AvatarMask UpperBodyMask;
  [SerializeField] Transform RootBone;

  PlayableGraph Graph;
  AnimationClipPlayable Lower;
  AnimationClipPlayable Upper;
  NativeArray<ReadWriteTransformHandle> Handles;

  NativeArray<ReadWriteTransformHandle> SetupMaskHandles(Transform root, Animator animator, AvatarMask mask) {
    var handles = new NativeArray<ReadWriteTransformHandle>(mask.transformCount, Allocator.Persistent);
    for (var i = 0; i < mask.transformCount; i++) {
      var active = mask.GetTransformActive(i);
      var path = mask.GetTransformPath(i);
      var transform = root.Find(path);
      handles[i] = ReadWriteTransformHandle.Bind(animator, transform);
    }
    return handles;
  }

  void Start() {
    // var desc = Avatar.humanDescription;
    // Graph = PlayableGraph.Create("Mesh-space Rotation");
    // Lower = AnimationClipPlayable.Create(Graph, LowerBodyClip);
    // Upper = AnimationClipPlayable.Create(Graph, UpperBodyClip);
    // var output = AnimationPlayableOutput.Create(Graph, )
    // Graph.Play();
  }

  void OnDestroy() {
    // Graph.Destroy();
  }

  void Update() {
  }
}