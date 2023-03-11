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
    try {
      await scope.Until(() => timeline.IsDone());
    } catch (Exception e) {
      Debug.LogWarning($"MeleeAttackAbility caught {e.Message}");
    } finally {
      outputs.ForEach(graph.DestroyOutput);
      graph.DestroySubgraph(timeline);
    }
  }
}