using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraTransition : MonoBehaviour {
  public RawImage Overlay;
  public Timeval TransitionDuration = Timeval.FromSeconds(1);

  IEnumerator Transition;

  void Start() => Transition = MakeTransition();

  IEnumerator MakeTransition() {
    for (var i = 0; i < TransitionDuration.Ticks; i++) {
      var color = Overlay.color;
      color.a = 1-(float)i/(float)TransitionDuration.Ticks;
      Overlay.color = color;
      yield return null;
    }
  }

  void FixedUpdate() {
    if (Transition == null || !Transition.MoveNext()) {
      Transition = null;
    }
  }
}