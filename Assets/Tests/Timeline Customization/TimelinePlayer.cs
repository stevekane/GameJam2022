using UnityEngine;
using UnityEngine.Timeline;

/*
2-23-23 Ideas for the day

Mixer tracks of concrete types could have the object that they affect assigned
directly to them. This means we would not need to create outputs of various kinds
but instead could just modify the referred to object directly and connect all
nodes in a chain to one single ScriptPlayableOutput.

The primary thing we get by NOT doing this is the uber-polymorphic userData API
but that isn't ultimately so useful in some sense because you still need to know
all the concrete types in order to correctly cast.
*/

public class TimelinePlayer : MonoBehaviour {
  [SerializeField] AnimationGraph AnimationGraph;
  [SerializeField] AudioGraph AudioGraph;
  [SerializeField] FixedGraph FixedGraph;
  [SerializeField] TimelineAsset TimelineAsset;

  void Start() {
    var fixedTimeline = FixedGraph.Play(TimelineAsset);
    var animationTimeline = AnimationGraph.PlayTimeline(TimelineAsset);
    var audioTimeline = AudioGraph.PlayTimeline(TimelineAsset);
  }
}