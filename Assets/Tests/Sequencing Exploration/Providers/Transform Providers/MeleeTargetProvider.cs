using UnityEngine;

[CreateAssetMenu(menuName = "TransformProviders/MeleeTarget")]
public class MeleeTargetProvider : TransformProvider {
  public override Transform Evaluate(GameObject gameObject) {
    if (gameObject.TryGetComponent(out MeleeAttackTargeting targeting)) {
      if (targeting.BestCandidate != null) {
        return targeting.BestCandidate.transform;
      } else {
        return null;
      }
    } else {
      return null;
    }
  }
}