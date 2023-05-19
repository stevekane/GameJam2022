using UnityEngine;
using UnityEngine.Playables;

public class GroundState : CharacterState {
  [SerializeField] CharacterState AirborneState;
  [SerializeField] BlendTreeAsset BlendTree;
  [SerializeField] float RateOfSpeedChange = 10;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return BlendTree.CreatePlayable(graph, owner);
  }

  public override void PostGroundingUpdate(float deltaTime) {
    if (!Motor.GroundingStatus.FoundAnyGround) {
      Controller.ChangeState(AirborneState);
    }
  }

  public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
    if (Controller.AllowRootRotation) {
      currentRotation = Controller.AnimationRotation * currentRotation;
    } else if (Controller.AllowRotating) {
      currentRotation = Controller.DirectRotation;
    }
  }

  public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    if (Controller.AllowRootMotion) {
      currentVelocity = Controller.AnimationVelocity;
    } else if (Controller.AllowMoving) {
      currentVelocity = Controller.DirectVelocity;
    } else {
      currentVelocity = Controller.PhysicsVelocity;
    }
  }

  public override void AfterCharacterUpdate(float deltaTime) {
    // Let's attempt a very hacky way of accessing our own playable
    var playable = AnimatorGraph.Mixer.GetInput(AnimatorGraph.CharacterStates.IndexOf(this));
    var blendTreePlayable = (ScriptPlayable<BlendTreeBehaviour>)playable;
    var blendTree = blendTreePlayable.GetBehaviour();
    blendTree.Value = Mathf.MoveTowards(blendTree.Value, Mathf.RoundToInt(Motor.BaseVelocity.magnitude), RateOfSpeedChange * deltaTime);
  }
}