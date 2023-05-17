using UnityEngine;
using KinematicCharacterController;

public abstract class CharacterState : MonoBehaviour, ICharacterController {
  public SimpleCharacterController Controller;
  public KinematicCharacterMotor Motor;

  public abstract void OnAnimatorMove();
  public abstract void AfterCharacterUpdate(float deltaTime);
  public abstract void BeforeCharacterUpdate(float deltaTime);
  public abstract bool IsColliderValidForCollisions(Collider coll);
  public abstract void OnDiscreteCollisionDetected(Collider hitCollider);
  public abstract void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);
  public abstract void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);
  public abstract void PostGroundingUpdate(float deltaTime);
  public abstract void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport);
  public abstract void UpdateRotation(ref Quaternion currentRotation, float deltaTime);
  public abstract void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);
}