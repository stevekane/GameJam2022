using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

/*
TESTS:

+ TWO CLIPS CONNECTED TO MIXER
- TWO CLIPS CONNECTED TO NOOP NODE PASSING THROUGH TO MIXER
+ TWO CLIPS CONNECTED TO NOOP. OUTPUTS CONNECTED TO NOOPS. NOOPS CONNECTED TO MIXER
*/
public class NoopBehavior : PlayableBehaviour {}

public class AudioMixerTests : MonoBehaviour {
  [SerializeField] AudioClip Clip1;
  [SerializeField] AudioClip Clip2;
  [SerializeField] AudioSource AudioSource1;
  [SerializeField] AudioSource AudioSource2;

  PlayableGraph Graph;

  void Start() {
    Graph = PlayableGraph.Create("AudioMixerTests");
    var mixer = AudioMixerPlayable.Create(Graph);
    var clip1 = AudioClipPlayable.Create(Graph, Clip1, true);
    var clip2 = AudioClipPlayable.Create(Graph, Clip2, true);
    var noop = ScriptPlayable<NoopBehavior>.Create(Graph);
    var noop1 = ScriptPlayable<NoopBehavior>.Create(Graph);
    var noop2 = ScriptPlayable<NoopBehavior>.Create(Graph);
    noop.SetTraversalMode(PlayableTraversalMode.Passthrough);
    noop.AddInput(clip1, 0, 1);
    noop.AddInput(clip2, 0, 1);
    noop.SetOutputCount(2);
    noop1.AddInput(noop, 0, 1);
    noop2.AddInput(noop, 1, 1);
    mixer.AddInput(noop1, 0, 1);
    mixer.AddInput(noop2, 0, 1);
    var output1 = AudioPlayableOutput.Create(Graph, "Output", AudioSource1);
    var output2 = AudioPlayableOutput.Create(Graph, "Output", AudioSource2);
    output1.SetSourcePlayable(mixer);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }
}