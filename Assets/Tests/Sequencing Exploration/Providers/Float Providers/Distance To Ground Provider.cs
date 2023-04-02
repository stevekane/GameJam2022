using UnityEngine;

[CreateAssetMenu(menuName = "FloatProviders/DistanceToGroundProvider")]
public class DistanceToGroundProvider : FloatProvider {
  public override float Evaluate(Animator animator) {
    var grounded = animator.GetComponent<SimpleAbilityManager>().Tags.HasFlag(AbilityTag.Grounded);
    var distance = animator.GetComponent<GroundDistance>().Value;
    return grounded ? 0 : distance;
  }
}