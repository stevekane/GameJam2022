using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class MeleeAttackAbility : MonoBehaviour {
  [Header("Configuration")]
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [Header("Components")]
  [SerializeField] LogicalTimeline LogicalTimeline;

  public async Task Attack(TaskScope scope) {
    var graph = LogicalTimeline.Graph;
    var bindings = TimelineTaskConfig.Bindings;
    var tracks = bindings.Select(binding => binding.Track);
    var timeline = TimelinePlayable.Create(graph, tracks, gameObject, false, false);
    var outputs = new List<PlayableOutput>(bindings.Length);
    var duration = TimelineTaskConfig.Asset.TickDuration(Timeval.FixedUpdatePerSecond);
    timeline.SetDuration(TimelineTaskConfig.Asset.duration); // TODO: not sure if correct. depends on durationmode
    timeline.SetTime(0);
    timeline.SetOutputCount(timeline.GetInputCount());
    foreach (var (track, port) in tracks.WithIndex()) {
      var playable = timeline.GetInput(port);
      var binding = bindings[port];
      var output = ScriptPlayableOutput.Create(graph, track.name);
      output.SetUserData(binding.Binding);
      output.SetSourcePlayable(timeline, port);
      outputs.Add(output);
    }
    try {
      for (var i = 0; i <= duration; i++) {
        await scope.ListenFor(LogicalTimeline.FixedTick);
      }
    } catch (Exception e) {
      Debug.LogWarning($"MeleeAttackAbility caught {e.Message}");
    } finally {
      LogicalTimeline.Disconnect();
      outputs.ForEach(graph.DestroyOutput);
      graph.DestroySubgraph(timeline);
    }
  }
}