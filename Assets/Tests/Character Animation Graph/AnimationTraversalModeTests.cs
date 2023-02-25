using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

struct AnimJob : IAnimationJob {
  public string Name;
  public int FrameId;
  public void ProcessRootMotion(AnimationStream stream) {
  }
  public void ProcessAnimation(AnimationStream stream) {
    Debug.Log($"{Name} {FrameId}");
  }
}

class SentinelBehavior : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    Debug.Log($"Process Frame Sentinel {info.frameId}");
  }
}

public class AnimationTraversalModeTests : MonoBehaviour {
  [SerializeField] AnimationClip Clip1;
  [SerializeField] AnimationClip Clip2;
  [SerializeField] Animator Animator1;
  [SerializeField] Animator Animator2;
  [SerializeField] PlayableTraversalMode TraversalMode;
  [SerializeField] bool ProcessInputs;

  PlayableGraph Graph;
  AnimationScriptPlayable Mixer;
  AnimationScriptPlayable PassThrough1;
  AnimationScriptPlayable PassThrough2;
  ScriptPlayable<SentinelBehavior> ScriptMixer;

  void Start() {
    Graph = PlayableGraph.Create("TraversalModeTests");
    Graph.Play();
    var output1 = AnimationPlayableOutput.Create(Graph, "Output 1", Animator1);
    var output2 = AnimationPlayableOutput.Create(Graph, "Output 2", Animator2);
    var scriptOutput = ScriptPlayableOutput.Create(Graph, "Script Output");
    var input1 = AnimationClipPlayable.Create(Graph, Clip1);
    var input2 = AnimationClipPlayable.Create(Graph, Clip2);
    var scriptSentinel = ScriptPlayable<SentinelBehavior>.Create(Graph);
    var scriptSentinel2 = ScriptPlayable<SentinelBehavior>.Create(Graph);
    PassThrough1 = AnimationScriptPlayable.Create(Graph, new AnimJob { Name = "ps1" });
    PassThrough2 = AnimationScriptPlayable.Create(Graph, new AnimJob { Name = "ps2" });
    PassThrough1.AddInput(input1, 0, 1);
    PassThrough2.AddInput(input2, 0, 1);
    ScriptMixer = ScriptPlayable<SentinelBehavior>.Create(Graph);
    ScriptMixer.AddInput(PassThrough1, 0, 1);
    ScriptMixer.AddInput(PassThrough2, 0, 1);
    ScriptMixer.AddInput(scriptSentinel, 0, 1);
    ScriptMixer.AddInput(scriptSentinel2, 0, 1);
    ScriptMixer.SetOutputCount(ScriptMixer.GetInputCount());
    output1.SetSourcePlayable(ScriptMixer, 0);
    output2.SetSourcePlayable(ScriptMixer, 1);
    scriptOutput.SetSourcePlayable(ScriptMixer, 2);
  }

  void Update() {
    PassThrough1.SetJobData(new AnimJob { Name = "PS1", FrameId = Time.frameCount });
    PassThrough2.SetJobData(new AnimJob { Name = "PS2", FrameId = Time.frameCount });
    ScriptMixer.SetTraversalMode(TraversalMode);
  }

  void OnDestroy() {
    Graph.Destroy();
  }
}