using UnityEngine;

[CreateAssetMenu(menuName = "Providers/RootMotion/MeleeTarget")]
public class MeleeTargetRootMotionProvider : RootMotionProvider {
  public float Distance;

  public override bool Active(GameObject gameObject) {
    return gameObject.GetComponent<MeleeAttackTargeting>().BestCandidate != null;
  }

  public override Vector3 Position(GameObject gameObject) {
    var target = gameObject.GetComponent<MeleeAttackTargeting>().BestCandidate.transform;
    var toTarget = (target.position - gameObject.transform.position).normalized;
    return target.position - toTarget * Distance;
  }

  public override Quaternion Rotation(GameObject gameObject) {
    var target = gameObject.GetComponent<MeleeAttackTargeting>().BestCandidate.transform;
    var toTarget = (target.position - gameObject.transform.position).normalized;
    return toTarget.magnitude > 0 ? Quaternion.LookRotation(toTarget) : gameObject.transform.rotation;
  }
}