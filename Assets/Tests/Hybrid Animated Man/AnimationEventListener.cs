using UnityEngine;

public class AnimationEventListener : MonoBehaviour {
  public EventSource<int> Event = new();
  public void AnimationEvent(int arg) {
    Event.Fire(arg);
  }
}
