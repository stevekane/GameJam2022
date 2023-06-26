using UnityEngine;
using KinematicCharacterController;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public abstract class SimpleAbility : MonoBehaviour, ICharacterController {
  public AbilityTag BlockActionsWith;
  public AbilityTag Tags;
  public AbilityTag AddedToOwner;
  public virtual bool IsRunning { get; set; }
  public virtual void Stop() {
    Tags = default;
    AddedToOwner = default;
    IsRunning = false;
  }

  protected SimpleAbilityManager AbilityManager;

  void OnEnable() {
    AbilityManager = GetComponentInParent<SimpleAbilityManager>();
    AbilityManager.AddAbility(this);
  }

  void OnDisable() {
    AbilityManager = GetComponentInParent<SimpleAbilityManager>();
    if (AbilityManager)
      AbilityManager.RemoveAbility(this);
  }

  public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {}
  public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {}
  public virtual void BeforeCharacterUpdate(float deltaTime) {}
  public virtual void PostGroundingUpdate(float deltaTime) {}
  public virtual void AfterCharacterUpdate(float deltaTime) {}
  public virtual bool IsColliderValidForCollisions(Collider coll) => true;
  public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
  public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
  public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}
  public virtual void OnDiscreteCollisionDetected(Collider hitCollider) {}
}