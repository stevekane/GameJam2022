using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using KinematicCharacterController;

[ExecuteInEditMode]
[DefaultExecutionOrder(ScriptExecutionGroups.Animation)]
public class AnimatorGraph : MonoBehaviour {
  [Header("Reads from")]
  [SerializeField] SimpleAbilityManager AbilityManager;
  [SerializeField] SimpleCharacterController Controller;
  [SerializeField] KinematicCharacterMotor Motor;
  [SerializeField] LocalTime LocalTime;

  [Header("Writes to")]
  [SerializeField] Animator Animator;

  [Header("Graph parts")]
  [SerializeField] BlendTreeAsset GroundedBlendTreeAsset;
  [SerializeField] SelectAsset AirborneSelectAsset;
  [SerializeField] TwistChain SpineTwistChain;

  [Header("Lean Animation")]
  [SerializeField] Transform HipTransform;
  [SerializeField] Transform RootTransform;
  [SerializeField, Range(0, 1)] float SnapThreshold = .1f;
  [SerializeField, Range(0,45)] float MaxForwardLean;
  [SerializeField, Range(0,45)] float MaxSidewaysLean;
  [SerializeField, Range(0,50)] float TurnAngleSpeed = 30;
  [SerializeField, Range(0,50)] float MaxTurnAngle = 20;
  [SerializeField, Range(0,50)] float ForwardElasticity = 10;
  [SerializeField, Range(0,50)] float MaxForwardAcceleration = 20;

  PlayableGraph Graph;
  Playable CurrentPlayable;
  AnimationLayerMixerPlayable LayerMixer;
  ScriptPlayable<CoreAnimation.SlotBehavior> FullBodySlot;
  ScriptPlayable<SelectBehavior> LocomotionStateSelect;
  ScriptPlayable<BlendTreeBehaviour> GroundBlendTree;
  ScriptPlayable<SelectBehavior> AirborneSelect;
  AnimationScriptPlayable GroundLean;
  AnimationScriptPlayable SpineTwistChainPlayable;
  AnimationPlayableOutput Output;

  Vector3 Forward;
  float TurnAngle;
  float ForwardSpeed;
  float ForwardAcceleration;

  void OnEnable() {
    RebuildGraph();
    TurnAngle = 0;
    Forward = transform.forward;
    ForwardSpeed = 0;
    ForwardAcceleration = 0;
  }

  void OnDisable() {
    if (Graph.IsValid())
      Graph.Destroy();
  }

  void FixedUpdate() {
    // New test of computing the instantaneous turn speed of the character
    {
      var forward = transform.forward;
      var turnAngle = Vector3.SignedAngle(Forward, forward, transform.up);
      var deltaAngle = turnAngle - TurnAngle;
      if (Mathf.Abs(deltaAngle) < SnapThreshold) {
        TurnAngle = turnAngle;
      } else {
        TurnAngle = Mathf.MoveTowards(TurnAngle, turnAngle, Time.fixedDeltaTime * TurnAngleSpeed);
      }
      Forward = forward;

      var forwardSpeed = Vector3.Project(Motor.BaseVelocity, transform.forward).magnitude;
      var forwardAcceleration = forwardSpeed-ForwardSpeed;
      if (Mathf.Abs(forwardAcceleration) < SnapThreshold) {
        ForwardSpeed = forwardSpeed;
        ForwardAcceleration = 0;
      } else {
        ForwardSpeed = ForwardSpeed + LocalTime.FixedDeltaTime * ForwardAcceleration;
        ForwardAcceleration = ForwardElasticity * forwardAcceleration;
      }
      var forwardLeanAngle = Mathf.Lerp(-MaxForwardLean, MaxForwardLean, Mathf.InverseLerp(-MaxForwardAcceleration, MaxForwardAcceleration, ForwardAcceleration));
      var sidewaysLeanAngle = Mathf.Lerp(-MaxSidewaysLean, MaxSidewaysLean, Mathf.InverseLerp(-MaxTurnAngle, MaxTurnAngle, TurnAngle));
      GroundLean.SetJobData(LeanJob.From(Animator, RootTransform, HipTransform, Mathf.Deg2Rad * forwardLeanAngle, Mathf.Deg2Rad * sidewaysLeanAngle));
    }

    var isGrounded = AbilityManager.HasFlag(AbilityTag.Grounded);
    var isAirborne = AbilityManager.HasFlag(AbilityTag.Airborne);
    var isHurt = AbilityManager.HasFlag(AbilityTag.Hurt);
    var velocity = Motor.BaseVelocity;
    var speed = Mathf.RoundToInt(velocity.magnitude);
    LocomotionStateSelect.GetBehaviour().CrossFade(isGrounded ? 0 : 1);
    // TODO: Don't rebind the StreamTransform every frame. just cache this on rebuild
    GroundBlendTree.GetBehaviour().Set(speed, 80);
    AirborneSelect.GetBehaviour().CrossFade(isHurt ? 1 : 0);
    // Steve - This is experimental to see if slot weight affects layer mixing blending
    LayerMixer.SetInputWeight(1, FullBodySlot.GetBehaviour().Weight);
    Graph.Evaluate(LocalTime.FixedDeltaTime);
  }

  void RebuildGraph() {
    if (Graph.IsValid())
      Graph.Destroy();
    Graph = PlayableGraph.Create(name);
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    // Create slot for dynamic playables
    FullBodySlot = ScriptPlayable<CoreAnimation.SlotBehavior>.Create(Graph, 1);
    // Create blendtree for grounded animations
    GroundBlendTree = (ScriptPlayable<BlendTreeBehaviour>)GroundedBlendTreeAsset.CreatePlayable(Graph, gameObject);
    // Creating procedural lean node following ground animations
    GroundLean = AnimationScriptPlayable.Create(Graph, LeanJob.From(Animator, RootTransform, HipTransform, MaxForwardLean, MaxSidewaysLean), 1);
    GroundLean.ConnectInput(0, GroundBlendTree, 0, 1);
    // Create select for airborne animations
    AirborneSelect = (ScriptPlayable<SelectBehavior>)AirborneSelectAsset.CreatePlayable(Graph, gameObject);
    // Create select for high level character states
    LocomotionStateSelect = ScriptPlayable<SelectBehavior>.Create(Graph, 1);
    // TODO: This is sort of unnatural. It'd be nice to implement the more idiomatic "addInput" somehow
    LocomotionStateSelect.GetBehaviour().Add(GroundLean);
    LocomotionStateSelect.GetBehaviour().Add(AirborneSelect);
    // Create layer mixer to combine locomotion and slot animations
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.AddInput(LocomotionStateSelect, 0, 1);
    LayerMixer.AddInput(FullBodySlot, 0, 1);
    SpineTwistChainPlayable = SpineTwistChain.CreatePlayable(Graph, Animator);
    SpineTwistChainPlayable.ConnectInput(0, LayerMixer, 0, 1);
    Output = AnimationPlayableOutput.Create(Graph, name, Animator);
    Output.SetSourcePlayable(SpineTwistChainPlayable);
  }

  public void Disconnect(AnimationClip clip) {
    if (Graph.IsValid()) {
      FullBodySlot.GetBehaviour().Disconnect(CurrentPlayable);
      CurrentPlayable = Playable.Null;
    }
  }

  public void Play(AnimationClip clip, double speed = 1) {
    CurrentPlayable = AnimationClipPlayable.Create(Graph, clip);
    CurrentPlayable.SetSpeed(speed);
    CurrentPlayable.SetDuration(clip.length);
    FullBodySlot.GetBehaviour().CrossFade(CurrentPlayable); // TODO: do something smarter with speed
  }

  #if UNITY_EDITOR
  public void Evaluate(double time, double duration) {
    // TODO: Very hacky. This is to enable preview mode... maybe think about this harder
    FullBodySlot.SetInputWeight(0, 1);
    CurrentPlayable.SetTime(time);
    CurrentPlayable.SetDuration(duration);
    Graph.Evaluate();
  }
  #endif
}