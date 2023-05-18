using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using KinematicCharacterController;

public abstract class CharacterState : MonoBehaviour, ICharacterController, IPlayableAsset {
  public AbilityTag ActiveTags;
  public AnimatorGraph AnimatorGraph;
  public SimpleAbilityManager AbilityManager;
  public SimpleCharacterController Controller;
  public KinematicCharacterMotor Motor;

  void OnEnable() {
    if (!AnimatorGraph.CharacterStates.Contains(this)) {
      AnimatorGraph.CharacterStates.Add(this);
      AnimatorGraph.RebuildGraph();
    }
  }

  void OnDisable() {
    if (AnimatorGraph.CharacterStates.Contains(this)) {
      AnimatorGraph.CharacterStates.Remove(this);
      AnimatorGraph.RebuildGraph();
    }
  }

  void OnValidate() {
    if (!AnimatorGraph.CharacterStates.Contains(this))
      AnimatorGraph.CharacterStates.Add(this);
    AnimatorGraph.RebuildGraph();
  }

  public virtual double duration => double.PositiveInfinity;
  public virtual IEnumerable<PlayableBinding> outputs => null;
  public virtual Playable CreatePlayable(PlayableGraph graph, GameObject owner) => Playable.Null;
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