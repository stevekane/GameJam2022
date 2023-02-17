using UnityEngine;
using UnityEngine.Playables;

public class LogTrackBehavior : PlayableBehaviour {
  public GameObject Source;
  public string Message;

  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    Debug.Log(Message, (GameObject)playerData);
  }
}