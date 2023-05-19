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
  [Header("Graph parts")]
  [SerializeField] BlendTreeAsset GroundedBlendTreeAsset;
  [SerializeField] SelectAsset AirborneSelectAsset;

  PlayableGraph Graph;
  AnimationLayerMixerPlayable LayerMixer;
  AnimationClipPlayable CurrentPlayable;
  ScriptPlayable<SelectBehavior> LocomotionStateSelect;
  ScriptPlayable<BlendTreeBehaviour> GroundBlendTree;
  ScriptPlayable<SelectBehavior> AirborneSelect;
  AnimationPlayableOutput Output;

  void OnEnable() {
    RebuildGraph();
  }

  void OnDisable() {
    if (Graph.IsValid())
      Graph.Destroy();
  }

  void OnValidate() {
    RebuildGraph();
  }

  void FixedUpdate() {
    var isGrounded = AbilityManager.HasFlag(AbilityTag.Grounded);
    var isAirborne = AbilityManager.HasFlag(AbilityTag.Airborne);
    var isHurt = AbilityManager.HasFlag(AbilityTag.Hurt);
    var velocity = Motor.BaseVelocity;
    var speed = Mathf.RoundToInt(velocity.magnitude);
    LocomotionStateSelect.GetBehaviour().CrossFade(isGrounded ? 0 : 1);
    GroundBlendTree.GetBehaviour().Value = speed;
    AirborneSelect.GetBehaviour().CrossFade(isHurt ? 1 : 0);
    Graph.Evaluate(Time.fixedDeltaTime);
  }

  public void RebuildGraph() {
    if (Graph.IsValid())
      Graph.Destroy();
    Graph = PlayableGraph.Create(name);
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    // Create slot for dynamic playables
    CurrentPlayable = AnimationClipPlayable.Create(Graph, null);
    // Create blendtree for grounded animations
    GroundBlendTree = (ScriptPlayable<BlendTreeBehaviour>)GroundedBlendTreeAsset.CreatePlayable(Graph, gameObject);
    // Create select for airborne animations
    AirborneSelect = (ScriptPlayable<SelectBehavior>)AirborneSelectAsset.CreatePlayable(Graph, gameObject);
    // Create select for high level character states
    LocomotionStateSelect = ScriptPlayable<SelectBehavior>.Create(Graph, 1);
    // TODO: This is sort of unnatural. It'd be nice to implement the more idiomatic "addInput" somehow
    LocomotionStateSelect.GetBehaviour().Add(GroundBlendTree);
    LocomotionStateSelect.GetBehaviour().Add(AirborneSelect);
    // Create layer mixer to combine locomotion and slot animations
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.AddInput(LocomotionStateSelect, 0, 1);
    LayerMixer.AddInput(CurrentPlayable, 0, 0);
    Output = AnimationPlayableOutput.Create(Graph, name, GetComponent<Animator>());
    Output.SetSourcePlayable(LayerMixer);
  }

  public void Connect(Playable playable, int outputPort) {
    Output.SetSourcePlayable(playable, outputPort);
  }

  public void Disconnect() {
    if (Graph.IsValid()) {
      Graph.DestroySubgraph(CurrentPlayable);
      CurrentPlayable = AnimationClipPlayable.Create(Graph, null);
      LayerMixer.SetInputWeight(1, 0);
    }
  }

  public void Evaluate(AnimationClip clip, double time, double duration, float weight) {
    if (CurrentPlayable.IsNull() || CurrentPlayable.GetAnimationClip() != clip) {
      Graph.DestroySubgraph(CurrentPlayable);
      CurrentPlayable = AnimationClipPlayable.Create(Graph, clip);
      CurrentPlayable.SetDuration(duration);
      LayerMixer.ConnectInput(1, CurrentPlayable, 0, 1);
    }
    LayerMixer.SetInputWeight(1, weight);
    CurrentPlayable.SetTime(time);
    #if UNITY_EDITOR
    if (!Application.isPlaying) {
      Graph.Evaluate();
    }
    #endif
  }
}