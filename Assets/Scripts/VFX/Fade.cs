using UnityEngine;

public class Fade : MonoBehaviour {
  [SerializeField] MeshRenderer MeshRenderer;
  [SerializeField] Timeval FadeDuration;

  int Duration;

  void FixedUpdate() {
    Duration++;
    if (Duration <= FadeDuration.Frames) {
      var color = MeshRenderer.material.color;
      var alpha = 1f-((float)Duration/(float)FadeDuration.Frames);
      color.a = alpha;
      MeshRenderer.material.color = color;
    }
  }
}