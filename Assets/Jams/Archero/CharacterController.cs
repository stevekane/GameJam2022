using UnityEngine;
using KinematicCharacterController;
using System;

namespace Archero {
  public class CharacterController : MonoBehaviour, ICharacterController {
    [SerializeField] KinematicCharacterMotor Motor;
    [SerializeField] SimpleAbilityManager AbilityManager;

    void Start() {
      Motor.CharacterController = this;
    }

    public void AfterCharacterUpdate(float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.AfterCharacterUpdate(deltaTime);
    }

    public void BeforeCharacterUpdate(float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.BeforeCharacterUpdate(deltaTime);
    }

    public bool IsColliderValidForCollisions(Collider coll) {
      var valid = true;
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          valid = valid && ability.IsColliderValidForCollisions(coll);
      return valid;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.OnDiscreteCollisionDetected(hitCollider);
    }

    public void OnGroundHit(
    Collider hitCollider,
    Vector3 hitNormal,
    Vector3 hitPoint,
    ref HitStabilityReport hitStabilityReport) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public void OnMovementHit(
    Collider hitCollider,
    Vector3 hitNormal,
    Vector3 hitPoint,
    ref HitStabilityReport hitStabilityReport) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public void PostGroundingUpdate(float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.PostGroundingUpdate(deltaTime);
    }

    public void ProcessHitStabilityReport(
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

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.UpdateRotation(ref currentRotation, deltaTime);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
      foreach (var ability in AbilityManager.Abilities)
        if (ability.IsRunning)
          ability.UpdateVelocity(ref currentVelocity, deltaTime);
    }

    public void Warp(Vector3 position, Quaternion rotation) {
      Motor.SetPositionAndRotation(position, rotation, true);
    }
  }
}