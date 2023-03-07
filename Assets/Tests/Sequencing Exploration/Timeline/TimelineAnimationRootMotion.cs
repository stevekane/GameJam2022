using UnityEngine;
using UnityEngine.Playables;

[ExecuteInEditMode]
public class TimelineAnimationRootMotion : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] PlayableDirector PlayableDirector;
  Vector3 TotalRootMotion;

  #if UNITY_EDITOR
  void OnAnimatorMove() {
    TotalRootMotion += Animator.deltaPosition;
    transform.Translate(Animator.deltaPosition);
  }
  void Update() {
    if (!Application.isPlaying && PlayableDirector.state != PlayState.Playing) {
      transform.Translate(-TotalRootMotion);
      TotalRootMotion = Vector3.zero;
    }
  }
  #endif
}