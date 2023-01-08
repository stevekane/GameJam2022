using System.Threading.Tasks;
using UnityEngine;

public class Grapple : Ability {
  [SerializeField] LineRenderer GrappleLine;
  [SerializeField] Timeval ThrowDuration;
  [SerializeField] Timeval PullDuration;
  [SerializeField] Timeval LaunchDuration;

  InlineEffect AimEffect => new(s => {
    s.CanMove = false;
  }, "Aim");

  InlineEffect ThrowEffect => new(s => {
    s.HasGravity = false;
    s.CanRotate = false;
    s.CanMove = false;
  }, "Throw and pull");

  public override async Task MainAction(TaskScope scope) {
    GrapplePoint target = null;
    var toTarget = float.MaxValue;
    foreach (var grapplePoint in GrapplePointManager.Instance.Points) {
      var isVisible = grapplePoint.transform.IsVisibleFrom(
        transform.position,
        Defaults.Instance.GrapplePointLayerMask,
        QueryTriggerInteraction.Collide);
      var toGrapplePoint = Vector3.Distance(transform.position, grapplePoint.transform.position);
      if (isVisible && toGrapplePoint < toTarget) {
        target = grapplePoint;
        toTarget = toGrapplePoint;
      }
    }
    if (target != null) {
      using (var throwEffect = Status.Add(ThrowEffect)) {
        GrappleLine.enabled = true;
        float fticks = (float)ThrowDuration.Ticks;
        for (var i = 0; i < ThrowDuration.Ticks; i++) {
          var interpolant = (float)i/fticks;
          var headPosition = Vector3.Lerp(transform.position, target.transform.position, interpolant);
          GrappleLine.SetPosition(0, transform.position + Vector3.up);
          GrappleLine.SetPosition(1, headPosition);
          target.Sources.Add(transform.position);
          await scope.Tick();
        }
        GrappleLine.enabled = false;
      }
      Mover.Move(target.transform.position-transform.position);
    } else {
      await scope.Tick();
    }
  }
}