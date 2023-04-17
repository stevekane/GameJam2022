using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[DefaultExecutionOrder(ScriptExecutionGroups.Animation)]
public class LogicalTimeline : MonoBehaviour {
  [SerializeField] LocalTime LocalTime;

  public PlayableGraph Graph;

  void Awake() {
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void FixedUpdate() {
    Graph.Evaluate(LocalTime.FixedDeltaTime);
  }

  // TODO: Possibly avoid allocation of outputs list somehow?
  // An internal pool perhaps or maybe some way of not needing
  // to create that list at all?
  public async Task Play(TaskScope scope, TimelineTaskConfig config) {
    var bindings = config.Bindings;
    var tracks = bindings.Select(binding => binding.Track).Where(track => !track.muted);
    var timeline = TimelinePlayable.Create(Graph, tracks, gameObject, false, false);
    var outputs = new List<PlayableOutput>(bindings.Length);
    var duration = config.Asset.duration;
    timeline.SetDuration(duration);
    timeline.SetTime(0);
    timeline.SetOutputCount(timeline.GetInputCount());
    foreach (var (track, port) in tracks.WithIndex()) {
      var trackMixer = timeline.GetInput(port);
      var binding = bindings[port];
      var output = ScriptPlayableOutput.Create(Graph, track.name);
      trackMixer.SetDuration(duration);
      output.SetUserData(binding.Binding);
      output.SetSourcePlayable(timeline, port);
      outputs.Add(output);
    }
    try {
      await scope.Until(() => timeline.IsDone());
    } finally {
      outputs.ForEach(Graph.DestroyOutput);
      Graph.DestroySubgraph(timeline);
    }
  }
}