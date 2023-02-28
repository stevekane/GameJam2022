using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class FixedGraph : MonoBehaviour {
  [SerializeField] Collider Hitbox;
  [SerializeField] WeaponTrail WeaponTrail;

  PlayableGraph Graph;
  ScriptPlayable<TimelinePlayable> CurrentTimeline;
  ScriptPlayableOutput HitboxOutput;
  ScriptPlayableOutput WeaponTrailOutput;

  void Awake() {
    Graph = PlayableGraph.Create($"FixedGraph ({name})");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    HitboxOutput = ScriptPlayableOutput.Create(Graph, "Hitbox Output");
    HitboxOutput.SetUserData(Hitbox);
    WeaponTrailOutput = ScriptPlayableOutput.Create(Graph, "WeaponTrail Output");
    WeaponTrailOutput.SetUserData(WeaponTrail);
  }

  void FixedUpdate() {
    if (!CurrentTimeline.IsNull() && CurrentTimeline.IsDone()) {
      Stop();
    }
    Graph.Evaluate(Time.fixedDeltaTime);
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  public ScriptPlayable<TimelinePlayable> Play(TimelineAsset timelineAsset) {
    var tracks = timelineAsset.Tracks(type => type == typeof(Collider) || type == typeof(WeaponTrail));
    var playable = TimelinePlayable.Create(Graph, tracks, gameObject, false, false);
    playable.SetTime(0);
    playable.SetDuration(timelineAsset.duration);
    playable.SetOutputCount(playable.GetInputCount());
    if (!CurrentTimeline.IsNull()) {
      Graph.DestroySubgraph(CurrentTimeline);
    }
    foreach (var (track, port) in tracks.WithIndex()) {
      var type = typeof(UnityEngine.Object);
      foreach (var binding in track.outputs) {
        type = binding.outputTargetType;
      }
      if (type == typeof(Collider)) {
        HitboxOutput.SetSourcePlayable(playable, port);
      } else if (type == typeof(WeaponTrail)) {
        WeaponTrailOutput.SetSourcePlayable(playable, port);
      }
    }
    CurrentTimeline = playable;
    return playable;
  }

  public void Stop() {
    Graph.DestroySubgraph(CurrentTimeline);
    CurrentTimeline = ScriptPlayable<TimelinePlayable>.Null;
  }
}