using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late+1)]
public class RootMotionTransfer : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  /*
  N.B. I HAVE SORT OF HACKED THIS UP.

  I want to test this as a constraint system that pushes the targets back
  from the attacker based on the attackers current position and the position
  of the target.

  I am hard-coding the distance to be 1 here for testing purposes.

  This initial code just takes our current position and uses it to determine
  how hard to push the target.


  IMPORTANT:
  There is a case you can test that will cause the solution above to NOT work.
  If you back your target into a wall, you will keep trying to move it backwards
  naively but will never succeed. Thus, the desired spacing maintained between
  you and the victim will be wrong and you will miss subsequent attacks.

  I am not sure overcoming this is worth the complexity but it's something to
  consider. To solve this, you would need to attempt to move the target.
  You would then need to observe its position.
  You would accumulate the error and use the max error to try to move yourself.
  */

  void Awake() {
    enabled = false;
  }

  void FixedUpdate() {
    const float DISTANCE = 1.5f;
    var position = transform.position;
    var forward = transform.forward;
    foreach (var target in MeleeAttackTargeting.Victims) {
      var targetPosition = target.transform.position;
      var deltaAlongForward = Vector3.Project(targetPosition - position, forward);
      var distanceAlongForward = deltaAlongForward.magnitude;
      var delta = (DISTANCE - distanceAlongForward) * deltaAlongForward.normalized;
      target.SendMessage("OnSynchronizedMove", delta);
    }
  }
}