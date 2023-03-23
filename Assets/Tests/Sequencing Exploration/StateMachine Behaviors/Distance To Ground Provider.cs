using UnityEngine;

[CreateAssetMenu(menuName = "FloatProviders/DistanceToGroundProvider")]
public class DistanceToGroundProvider : FloatProvider {
  public override float Evaluate(Animator animator) {
    var grounded = animator.GetComponent<Traditional.Grounded>().Value;
    var distance = animator.GetComponent<Traditional.GroundDistance>().Value;
    return grounded ? 0 : distance;
  }
}