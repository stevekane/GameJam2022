using UnityEngine;
using UnityEngine.AI;

public class AIMover : Mover {
  public new void Awake() {

  }
  public new void FixedUpdate() {
    var NavMeshAgent = GetComponent<NavMeshAgent>();
    var AnimationDriver = GetComponent<AnimationDriver>();
    var Status = GetComponent<Status>();
    var animator = AnimationDriver.Animator;
    var orientedVelocity = Quaternion.Inverse(transform.rotation)*NavMeshAgent.velocity.normalized;
    animator.SetFloat("RightVelocity", orientedVelocity.x);
    animator.SetFloat("ForwardVelocity", orientedVelocity.z);
    animator.SetBool("IsGrounded", Status.IsGrounded);
    animator.SetBool("IsHurt", Status.IsHurt);
    AnimationDriver.SetSpeed(1);
  }

  public new void OnDrawGizmos() {
    var NavMeshAgent = GetComponent<NavMeshAgent>();
    Gizmos.color = NavMeshAgent.isOnOffMeshLink ? Color.green : Color.red;
    Gizmos.DrawRay(transform.position + 4 * Vector3.up, 4 * Vector3.down);
  }
}