using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class MeleeAttackAbility : SimpleAbility {
  [SerializeField] TimelineTaskConfig TimelineTaskConfig;
  [SerializeField] LogicalTimeline LogicalTimeline;

  TaskScope Scope;

  public override void OnRun() {
    Scope = new();
    Scope.Run(Attack);
    IsRunning = true;
    //StartCoroutine(AttackEnumerator());
  }

  public override void OnStop() {
    Scope.Dispose();
    Scope = null;
    IsRunning = false;
    //Outputs.ForEach(LogicalTimeline.Graph.DestroyOutput);
    //LogicalTimeline.Graph.DestroySubgraph(Timeline);
    //StopAllCoroutines();
  }

  ScriptPlayable<TimelinePlayable> Timeline;
  List<PlayableOutput> Outputs = new List<PlayableOutput>();
  IEnumerator AttackEnumerator() {
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
    Timeline = timeline;
    Outputs.Clear();
    Outputs = outputs;
    yield return new WaitUntil(() => timeline.IsDone());
    Stop();
  }

  async Task Attack(TaskScope scope) {
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
      Stop();
    } finally {
      outputs.ForEach(graph.DestroyOutput);
      graph.DestroySubgraph(timeline);
    }
  }
}