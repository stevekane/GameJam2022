using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[Serializable]
public struct SkeletonMap {
  public string Root;
  public string Bone1;
  public string Bone2;
}

[Serializable]
public struct SkeletonMask {
  public float Root;
  public float Bone1;
  public float Bone2;
}

public struct QuaternionHandle {
  public PropertyStreamHandle x;
  public PropertyStreamHandle y;
  public PropertyStreamHandle z;
  public PropertyStreamHandle w;
}

public struct SkeletonHandles {
  public PropertyStreamHandle Root;
  public PropertyStreamHandle Bone1;
  public PropertyStreamHandle Bone2;
}

public class RetargetJob : IAnimationJob {
  public AnimationClip Clip;
  public SkeletonHandles SkeletonHandles;
  public SkeletonMap SkeletonMap;
  public SkeletonMask SkeletonMask;

  public void ProcessRootMotion(AnimationStream stream) {
    // TODO: what to do here for correctness?
  }

  public void ProcessAnimation(AnimationStream stream) {
    var position = Vector3.zero;
    var rotation = Quaternion.identity;
    var scale = Vector3.zero;
    var input = stream.GetInputStream(0);
  }
}

public class EditorAvatarCreator : MonoBehaviour {
  [SerializeField] AnimationClip AnimationClip;
  [SerializeField] Animator Animator;
  [SerializeField] Animator Animator2;
  [SerializeField] GameObject ManualRoot;
  [SerializeField] GameObject RuntimeRoot;
  [SerializeField] SkeletonMap SkeletonMap;
  [SerializeField] SkeletonMask SkeletonMask;

  PlayableGraph Graph;

  void Start() {
    // Run-time root setup
    var bone1 = new GameObject("Bone 1");
    var bone2 = new GameObject("Bone 2");
    bone1.transform.SetParent(RuntimeRoot.transform);
    bone2.transform.SetParent(bone1.transform);
    bone1.transform.localPosition = Vector3.up;
    bone2.transform.localPosition = Vector3.up;
    RuntimeRoot.GetComponent<Animator>().Rebind();

    Graph = PlayableGraph.Create("Retargeting");
    var clip = AnimationClipPlayable.Create(Graph, AnimationClip);
    var output = AnimationPlayableOutput.Create(Graph, "Animator output", Animator);
    var output2 = AnimationPlayableOutput.Create(Graph, "Animator output", Animator2);
    output.SetSourcePlayable(clip, 0);
    output2.SetSourcePlayable(clip, 1);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void Update() {
    AnimationClip.SampleAnimation(ManualRoot, Time.time % AnimationClip.length);
  }
}