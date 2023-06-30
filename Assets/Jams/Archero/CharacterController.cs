using UnityEngine;
using KinematicCharacterController;

namespace Archero {
  public class CharacterController : MonoBehaviour, ICharacterController {
    public KinematicCharacterMotor Motor;
    public SimpleAbilityManager AbilityManager;

    public virtual void Start() {
      Motor.CharacterController = this;
    }

    public virtual void AfterCharacterUpdate(float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.AfterCharacterUpdate(deltaTime);
    }

    public virtual void BeforeCharacterUpdate(float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.BeforeCharacterUpdate(deltaTime);
    }

    public virtual bool IsColliderValidForCollisions(Collider coll) {
      var valid = true;
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          valid = valid && ability.IsColliderValidForCollisions(coll);
      return valid;
    }

    public virtual void OnDiscreteCollisionDetected(Collider hitCollider) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.OnDiscreteCollisionDetected(hitCollider);
    }

    public virtual void OnGroundHit(
    Collider hitCollider,
    Vector3 hitNormal,
    Vector3 hitPoint,
    ref HitStabilityReport hitStabilityReport) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public virtual void OnMovementHit(
    Collider hitCollider,
    Vector3 hitNormal,
    Vector3 hitPoint,
    ref HitStabilityReport hitStabilityReport) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public virtual void PostGroundingUpdate(float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.PostGroundingUpdate(deltaTime);
    }

    public virtual void ProcessHitStabilityReport(
    Collider hitCollider,
    Vector3 hitNormal,
    Vector3 hitPoint,
    Vector3 atCharacterPosition,
    Quaternion atCharacterRotation,
    ref HitStabilityReport hitStabilityReport) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.ProcessHitStabilityReport(
            hitCollider,
            hitNormal,
            hitPoint,
            atCharacterPosition,
            atCharacterRotation,
            ref hitStabilityReport);
    }

    public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.UpdateRotation(ref currentRotation, deltaTime);
    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.UpdateVelocity(ref currentVelocity, deltaTime);
    }

    public void Warp(Vector3 position, Quaternion rotation) {
      Motor.SetPositionAndRotation(position, rotation, true);
    }
  }
}