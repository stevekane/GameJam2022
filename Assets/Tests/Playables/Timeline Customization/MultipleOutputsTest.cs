using UnityEngine;
using UnityEngine.Playables;

public class Logger : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var source = (AudioSource)playerData;
    Debug.Log($"{source.name} {info.frameId}");
  }
}

public class MultipleOutputsTest : MonoBehaviour {
  [SerializeField] AudioSource Source1;
  [SerializeField] AudioSource Source2;

  PlayableGraph Graph;

  void Start() {
    Graph = PlayableGraph.Create("Multiple outputs");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.DSPClock);
    // Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    var output1 = ScriptPlayableOutput.Create(Graph, "Source 1");
    var output2 = ScriptPlayableOutput.Create(Graph, "Source 2");
    var passthrough = ScriptPlayable<Logger>.Create(Graph);
    passthrough.SetTraversalMode(PlayableTraversalMode.Passthrough);
    passthrough.SetOutputCount(2);
    output1.SetSourcePlayable(passthrough, 0);
    output1.SetUserData(Source1);
    output2.SetSourcePlayable(passthrough, 1);
    output2.SetUserData(Source2);
  }

  void FixedUpdate() {
    // Graph.Evaluate(Time.fixedDeltaTime);
  }

  void OnDestroy() {
    Graph.Destroy();
  }
}