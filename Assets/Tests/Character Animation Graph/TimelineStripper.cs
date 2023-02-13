using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class TimelineStripper : MonoBehaviour {
  [SerializeField] CharacterAnimationGraph AnimationGraph;
  [SerializeField] TimelineAsset TimelineAsset;

  void Start() {
    // Steve - This approach does not seem to work. Here I am trying to disconnect the inputs
    // from the timeline and instead connect them directly to the slots of the CharacterAnimGraph
    // this works as you would expect but the connected tracks don't seem to play properly.
    // my guess is that the timeline component itself controls/sets the weights on the tracks
    // based on their data to determine when they should be active.
    var graph = AnimationGraph.Graph;
    var slot = AnimationGraph.DefaultSlot;
    var timelinePlayable = TimelineAsset.CreatePlayable(graph, gameObject);
    var inputCount = timelinePlayable.GetInputCount();
    for (var o = 0; o < TimelineAsset.outputTrackCount; o++) {
      var track = TimelineAsset.GetOutputTrack(o);
      var input = timelinePlayable.GetInput(o);
      timelinePlayable.DisconnectInput(o);
      foreach (var output in track.outputs) {
        var targetType = output.outputTargetType;
        if (targetType == typeof(Animator)) {
          slot.PlayAnimation(input, 0);
        } else if (targetType == typeof(AudioSource)) {
          // N.B. The noop shim between timeline and mixer enables a mixer to play multiple
          // outputs coming from a single timeline node... very odd situation
          var noop = ScriptPlayable<NoopBehavior>.Create(graph);
          noop.AddInput(input, 0, 1);
          slot.PlayAudio(noop, 0);
        } else {
          graph.DestroySubgraph(input);
        }
      }
    }
  }
}