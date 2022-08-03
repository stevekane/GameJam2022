using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventTimeline", menuName = "Animation/EventTimeline")]
public class FrameEventTimeline : ScriptableObject {
  public AnimationClip Clip;
  public List<FrameEvent> Events;
}