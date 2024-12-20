using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Audio;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class CharacterAnimationGraphCharacterController : MonoBehaviour {
  [SerializeField] CharacterAnimationGraph AnimationGraph;
  [SerializeField] AudioClip AudioClip;
  [SerializeField] AnimationClip AnimationClip;
  [SerializeField] TimelineAsset TimelineAsset;

  Playable CurrentPlayable = Playable.Null;

  static Playable Play(
  TimelineAsset timelineAsset,
  PlayableGraph graph,
  SlotBehavior slot,
  GameObject gameObject) {
    var timelinePlayable = timelineAsset.CreatePlayable(graph, gameObject);
    var outputTrackCount = timelineAsset.outputTrackCount;
    // N.B. You MUST do this to open up all the ports associated with inputs/tracks
    UnityEngine.Playables.PlayableExtensions.SetOutputCount(timelinePlayable, 5);
    // loop over all the tracks in the playable asset
    for (var outputTrackIndex = 0; outputTrackIndex < outputTrackCount; outputTrackIndex++) {
      var track = timelineAsset.GetOutputTrack(outputTrackIndex);
      // loop over all the outputs for a given track
      foreach (var output in track.outputs) {
        // use the type of the output to determine how to play this track on the graph
        var targetType = output.outputTargetType;
        if (targetType == typeof(Animator)) {
          slot.PlayAnimation(timelinePlayable, outputTrackIndex);
        } else if (targetType == typeof(AudioSource)) {
          // N.B. The noop shim between timeline and mixer enables a mixer to play multiple
          // outputs coming from a single timeline node... very odd situation
          var noop = ScriptPlayable<NoopBehavior>.Create(graph);
          noop.AddInput(timelinePlayable, outputTrackIndex, 1);
          slot.PlayAudio(noop, 0);
        }
      }
    }
    return timelinePlayable;
  }

  void Update() {
    if (CurrentPlayable.IsValid() && CurrentPlayable.IsDone()) {
      AnimationGraph.Graph.DestroySubgraph(CurrentPlayable);
      CurrentPlayable = Playable.Null;
    }
    if (CurrentPlayable.IsNull()) {
      if (Input.GetKeyDown(KeyCode.Q)) {
        CurrentPlayable = AudioClipPlayable.Create(AnimationGraph.Graph, AudioClip, false);
        AnimationGraph.DefaultSlot.PlayAudio(CurrentPlayable);
      }
      if (Input.GetKeyDown(KeyCode.W)) {
        CurrentPlayable = AnimationClipPlayable.Create(AnimationGraph.Graph, AnimationClip);
        CurrentPlayable.SetDuration(AnimationClip.length);
        AnimationGraph.DefaultSlot.PlayAnimation(CurrentPlayable);
      }
      if (Input.GetKeyDown(KeyCode.E)) {
        CurrentPlayable = Play(TimelineAsset, AnimationGraph.Graph, AnimationGraph.DefaultSlot, gameObject);
      }
    }
  }
}