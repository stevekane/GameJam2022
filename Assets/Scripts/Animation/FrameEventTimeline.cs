using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventTimeline", menuName = "Animations/EventTimeline")]
public class FrameEventTimeline : ScriptableObject {
  public AnimationClip Clip;
  public List<FrameEvent> Events;
}