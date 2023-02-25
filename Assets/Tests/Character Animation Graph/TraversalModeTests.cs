using UnityEngine;
using UnityEngine.Playables;

/*
Test what affect traversal mode has on a multi-output node.

My hypothesis:
Mixer nodes will evaluate all of their connected inputs for a given connected output.
Passthrough nodes will evaluate only the connected input of the same index as a connected output.

We need an output.
We need a custom node with multiple outputs.
We need to connect two test nodes to the inputs of the custom node.
We want to observe who gets ProcessFrame called on them
*/

class SingleOutput : PlayableBehaviour {
  public string name;
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    Debug.Log("${name} {info.frameId}");
  }
}
class MultiOutput : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    Debug.Log($"MultiOutput {info.frameId}");
  }
}

public class TraversalModeTests : MonoBehaviour {
  [SerializeField] PlayableTraversalMode TraversalMode;

  PlayableGraph Graph;
  ScriptPlayable<MultiOutput> Mixer;

  void Start() {
    Graph = PlayableGraph.Create("TraversalModeTests");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    var output = ScriptPlayableOutput.Create(Graph, "Output");
    var input1 = ScriptPlayable<SingleOutput>.Create(Graph);
    var input2 = ScriptPlayable<SingleOutput>.Create(Graph);
    Mixer = ScriptPlayable<MultiOutput>.Create(Graph);
    Mixer.AddInput(input1, 0, 1);
    Mixer.AddInput(input2, 0, 1);
    output.SetSourcePlayable(Mixer, 0);
  }

  void Update() {
    Mixer.SetTraversalMode(TraversalMode);
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  [ContextMenu("Evaluate")]
  public void Evaluate() {
    Graph.Evaluate();
  }
}