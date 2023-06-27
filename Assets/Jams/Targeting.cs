using UnityEngine;

namespace Archero {
  public class Targeting : MonoBehaviour {
    [Header("Scoring Config")]
    [SerializeField] float MaxDistance = 20;
    [SerializeField] float MaxAngle = 90;
    [Header("Scoring Weights")]
    [SerializeField] float DistanceWeight = 1;
    [SerializeField] float AngleWeight = 1;
    [SerializeField] float LineOfSightWeight = 1;
    [SerializeField] LayerMask WallAndMobLayerMask;

    bool IsVisible(Mob mob) {
      var toMobDelta = mob.transform.position-transform.position;
      var distance = toMobDelta.magnitude;
      var toMob = toMobDelta.normalized;
      var didHit = Physics.Raycast(
        transform.position,
        toMob,
        out RaycastHit hit,
        distance,
        WallAndMobLayerMask,
        QueryTriggerInteraction.Collide);
      if (didHit && hit.collider.TryGetComponent(out Hurtbox hurtbox)) {
        return hurtbox.Owner.gameObject == mob.gameObject;
      } else {
        return false;
      }
    }

    float Score(Mob mob) {
      var distance = Vector3.Distance(mob.transform.position, transform.position);
      var angle = Vector3.Angle(transform.forward, mob.transform.position-transform.position);
      var los = IsVisible(mob);
      var distanceScore = 1-Mathf.InverseLerp(0, MaxDistance, Mathf.Min(distance, MaxDistance));
      var angleScore = 1-Mathf.InverseLerp(0, MaxAngle, Mathf.Min(angle, MaxAngle));
      return DistanceWeight * distanceScore + AngleWeight * angleScore + (los ? LineOfSightWeight : 0);
    }

    public Mob BestTarget {
      get {
        Mob bestTarget = null;
        float bestScore = 0;
        foreach (var mob in MobManager.Instance.Mobs) {
          var score = Score(mob);
          if (score > bestScore) {
            bestTarget = mob;
            bestScore = score;
          }
        }
        return bestTarget;
      }
    }
  }
}