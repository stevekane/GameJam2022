using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class SlideAblity : SimpleAbility {
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] float Distance = 10;
  [SerializeField] LogicalTimeline LogicalTimeline;

  TaskScope Scope;

  public override void OnRun() {
    Scope = new();
    Scope.Run(Slide);
  }

  public override void OnStop() {
    Scope.Dispose();
  }

  async Task Slide(TaskScope scope) {
    var graph = LogicalTimeline.Graph;
    var bindings = TimelineTaskConfig.Bindings;
    var tracks = bindings.Select(binding => binding.Track).Where(track => !track.muted);
    var timeline = TimelinePlayable.Create(graph, tracks, gameObject, false, false);
    var outputs = new List<PlayableOutput>(bindings.Length);
    var duration = TimelineTaskConfig.Asset.duration;
    timeline.SetDuration(duration);
    timeline.SetTime(0);
    timeline.SetOutputCount(timeline.GetInputCount());
    foreach (var (track, port) in tracks.WithIndex()) {
      var trackMixer = timeline.GetInput(port);
      var binding = bindings[port];
      var output = ScriptPlayableOutput.Create(graph, track.name);
      trackMixer.SetDuration(duration);
      output.SetUserData(binding.Binding);
      output.SetSourcePlayable(timeline, port);
      outputs.Add(output);
    }
    IsRunning = true;
    var velocity = (Distance / (float)timeline.GetDuration()) * transform.forward;
    try {
      while (!timeline.IsDone()) {
        DirectMotion.IsActive(true, 1);
        DirectMotion.Override(Time.deltaTime * velocity, 1);
        await scope.Tick();
      }
    } finally {
      IsRunning = false;
      outputs.ForEach(graph.DestroyOutput);
      graph.DestroySubgraph(timeline);
    }
  }
}