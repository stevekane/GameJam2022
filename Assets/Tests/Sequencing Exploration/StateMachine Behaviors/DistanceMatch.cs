using UnityEngine;

public class DistanceMatch : StateMachineBehaviour {
  [SerializeField] float MaxDistance;
  [SerializeField] string ParameterName;
  [SerializeField] Timeval ClipDuration; // Can I get this in code somehow?
  [SerializeField] FloatProvider DistanceProvider;

  public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    animator.SetFloat(ParameterName, 1);
  }

  public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    var distance = DistanceProvider.Evaluate(animator);
    var time = ClipDuration.Seconds;
    var dt = Time.deltaTime;
    var currentTime = ClipDuration.Seconds * stateInfo.normalizedTime;
    var targetTime = ClipDuration.Seconds * Mathf.InverseLerp(MaxDistance, 0, distance);
    var animationSpeed = (targetTime - currentTime) / dt;
    animator.SetFloat(ParameterName, animationSpeed);
  }

  public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    animator.SetFloat(ParameterName, 1);
  }
}