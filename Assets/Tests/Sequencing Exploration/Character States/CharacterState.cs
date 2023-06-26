using UnityEngine;
using KinematicCharacterController;

public abstract class CharacterState : MonoBehaviour, ICharacterController {
  public AbilityTag ActiveTags;
  public AnimatorGraph AnimatorGraph;
  public SimpleAbilityManager AbilityManager;
  public SimpleCharacterController Controller;
  public KinematicCharacterMotor Motor;

  public virtual void OnEnter() {}
  public virtual void OnExit() {}
  public virtual void OnAnimatorMove() {}
  public virtual void AfterCharacterUpdate(float deltaTime) {}
  public virtual void BeforeCharacterUpdate(float deltaTime) {}
  public virtual bool IsColliderValidForCollisions(Collider coll) => true;
  public virtual void OnDiscreteCollisionDetected(Collider hitCollider) {}
  public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
  public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
  public virtual void PostGroundingUpdate(float deltaTime) {}
  public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}
  public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {}
  public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {}
}