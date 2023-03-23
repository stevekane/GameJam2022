using UnityEngine;

[CreateAssetMenu(menuName = "FloatProviders/YSpeedProvider")]
public class YSpeedProvider: FloatProvider {
  public override float Evaluate(Animator animator) {
    return animator.GetComponent<Velocity>().Value.y;
  }
}