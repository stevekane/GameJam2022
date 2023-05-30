using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Collections;

public class TwistChain : MonoBehaviour {
  [SerializeField, Range(0,1)] float Weight = 1;
  [SerializeField] Transform Root;
  [SerializeField] Transform Tip;
  [SerializeField] Transform RootEffector;
  [SerializeField] Transform TipEffector;
  [SerializeField] AnimationCurve WeightCurve = AnimationCurve.Linear(0,0,1,1);

  TwistChainConstraintJob Job;

  public AnimationScriptPlayable CreatePlayable(PlayableGraph graph, Animator animator) {
    var chain = ConstraintsUtils.ExtractChain(Root, Tip);
    var steps = ConstraintsUtils.ExtractSteps(chain);
    var Job = new TwistChainConstraintJob();
    Job.jobWeight = FloatProperty.Bind(animator, this, "Weight");
    Job.chain = new NativeArray<ReadWriteTransformHandle>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    Job.steps = new NativeArray<float>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    Job.weights = new NativeArray<float>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    Job.rotations = new NativeArray<Quaternion>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    Job.rootTarget = ReadWriteTransformHandle.Bind(animator, RootEffector);
    Job.tipTarget = ReadWriteTransformHandle.Bind(animator, TipEffector);
    for (int i = 0; i < chain.Length; ++i) {
      Job.chain[i] = ReadWriteTransformHandle.Bind(animator, chain[i]);
      Job.steps[i] = steps[i];
      Job.weights[i] = Mathf.Clamp01(WeightCurve.Evaluate(steps[i]));
    }
    Job.rotations[0] = Quaternion.identity;
    Job.rotations[chain.Length - 1] = Quaternion.identity;
    for (int i = 1; i < chain.Length - 1; ++i) {
      Job.rotations[i] = Quaternion.Inverse(Quaternion.Lerp(chain[0].rotation, chain[chain.Length - 1].rotation, Job.weights[i])) * chain[i].rotation;
    }
    var playable = AnimationScriptPlayable.Create(graph, Job, 1);
    return playable;
  }

  void OnDestroy() {
    Job.chain.Dispose();
    Job.steps.Dispose();
    Job.weights.Dispose();
    Job.rotations.Dispose();
  }
}