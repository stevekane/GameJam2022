using UnityEngine;
using UnityEngine.Playables;

/*
OnCreate/Destroy don't have access the playerData since there could be multiple
outputs ultimately connected to this node in which case the playerdata would vary
for each processing of the graph.

As such, using these lifecycle hooks to set default values on some targeted object
doesn't really work.

When we have code that is writing to some data in our existing Task-based solution,
we can define a finally block which will always run when the task ends and can set
some kind of universal post condition for that task.

In general, we want a similar thing for Timelines that are driving logic. The question is,
where do we put this code?

One place we could put this is outside the Timeline/Playable ecosystem altogether.

For example, any pre/post conditions you wish to enforce would still be done in code
before and after the timeline plays. This is probably the simplest way of doing this
given the fact that the pipeline for processing each output connected to a node
doesn't give an easy hook for "first time you process me/last time you process me".
*/
public class HitBoxTrackMixer : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var hitbox = (Collider)playerData;
    if (!hitbox)
      return;
    var inputCount = playable.GetInputCount();
    var active = false;
    for (var i = 0; i < inputCount; i++) {
      active = active || playable.GetInputWeight(i) > 0;
    }
    hitbox.enabled = active;
  }
}