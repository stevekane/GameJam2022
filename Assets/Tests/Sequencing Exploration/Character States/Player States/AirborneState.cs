using UnityEngine;
using UnityEngine.Playables;

public class AirborneState : CharacterState {
  [SerializeField] CharacterState GroundState;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Gravity Gravity;
  [SerializeField] SelectAsset Select;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return Select.CreatePlayable(graph, owner);
  }

  public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
    currentRotation = Controller.DirectRotation;
  }

  public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    currentVelocity = Controller.PhysicsVelocity;
  }

  public override void PostGroundingUpdate(float deltaTime) {
    if (Motor.GroundingStatus.FoundAnyGround) {
      Controller.ChangeState(GroundState);
    }
  }

  public override void AfterCharacterUpdate(float deltaTime) {
    // Let's attempt a very hacky way of accessing our own playable
    var playable = AnimatorGraph.Mixer.GetInput(AnimatorGraph.CharacterStates.IndexOf(this));
    var selectPlayable = (ScriptPlayable<SelectBehavior>)playable;
    var select = selectPlayable.GetBehaviour();
    var isHurt = AbilityManager.HasFlag(AbilityTag.Hurt);
    select.CrossFade(isHurt ? 1 : 0);
  }
}