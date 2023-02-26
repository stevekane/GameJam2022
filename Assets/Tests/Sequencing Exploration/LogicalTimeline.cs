using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [SerializeField] RuntimeAnimatorController AnimatorController;
  [SerializeField] InputManager InputManager;
  [SerializeField] AnimationClip Clip;
  [SerializeField] float BlendInFraction = .05f;
  [SerializeField] float BlendOutFraction = .05f;
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;

  TaskScope Scope;
  AnimationLayerMixerPlayable LayerMixer;
  PlayableGraph Graph;

  /*
  Findings:
    AnimatorController probably should not be supplied to the Animator. Instead,
    get a reference to the RuntimeAnimatorController yourself and attach it to
    your graph. This prevents the Graph and the Animator component from double-updating
    the Controller causing some weird issues when playing w/ a graph.

  Notes:

    Start/Awake and OnDestroy are not symmetric.
      Start/Awake won't be called if script is disabled but OnDestroy IS called

    Animator STARTING in Physics update Mode will be glitchy when you start an animation
    Animator update mode changed to Pysics in code DOES work. I have tested this indeed
    is updating in Physics for both the animator and the playable;
  */

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Scope = new TaskScope();
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    var animController = AnimatorControllerPlayable.Create(Graph, AnimatorController);
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.SetInputCount(2);
    LayerMixer.ConnectInput(0, animController, 0, 1);
    var output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    output.SetSourcePlayable(LayerMixer);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    Graph.Evaluate(Time.fixedDeltaTime);
    FixedFrame++;
    FixedTick.Fire();
  }

  void OnAnimatorMove() {
    Controller.Move(Animator.deltaPosition);
  }

  void StartAttack() {
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    // StartCoroutine(AttackAnimatorOnTicks());
    // StartCoroutine(AttackAnimator());
    Scope.Start(Attack);
  }

  async Task Attack(TaskScope scope) {
    var frame0 = FixedFrame;
    var time0 = Time.time;
    var playable = AnimationClipPlayable.Create(Graph, Clip);
    playable.SetTime(0);
    playable.SetDuration(Clip.length);
    LayerMixer.SetInputWeight(1, 1);
    try {
      LayerMixer.DisconnectInput(1);
      LayerMixer.ConnectInput(1, playable, 0, 1);
      var ticks = Mathf.RoundToInt(Clip.length * Timeval.FixedUpdatePerSecond);
      for (var i = 0; i < ticks; i++) {
        var fraction = (float)i / (float)ticks;
        var weight = BlendWeight(BlendInFraction, BlendOutFraction, fraction) ;
        LayerMixer.SetInputWeight(1, weight);
        await scope.ListenFor(FixedTick);
      }
    } catch (Exception e) {
      Debug.LogWarning(e.Message);
    } finally {
      LayerMixer.DisconnectInput(1);
      LayerMixer.SetInputWeight(1, 0);
      playable.Destroy();
      Debug.Log($"{FixedFrame-frame0} Fixed Frames {Time.time-time0} seconds");
    }
  }

  // Uses the passage of time to ultimately exit the process
  IEnumerator AttackAnimator() {
    Animator.SetTrigger("Attack");
    var frame0 = FixedFrame;
    var time0 = Time.time;
    var duration = Clip.length;
    var elapsed = 0f;
    while (elapsed < duration) {
      elapsed += Time.deltaTime;
      yield return null;
    }
    Debug.Log($"{FixedFrame-frame0} Fixed Frames {Time.time-time0} seconds");
  }

  // Waits a number of ticks corresponding to the length in fixed ticks of the clip
  IEnumerator AttackAnimatorOnTicks() {
    Debug.Log(Time.fixedDeltaTime);
    Animator.SetTrigger("Attack");
    var frame0 = FixedFrame;
    var time0 = Time.time;
    var duration = Clip.length;
    var ticks = Mathf.RoundToInt(Clip.length * Timeval.FixedUpdatePerSecond);
    for (var i = 0; i < ticks; i++) {
      yield return new WaitForFixedUpdate();
    }
    Debug.Log($"{FixedFrame-frame0} Fixed Frames {Time.time-time0} seconds");
  }

  float BlendWeight(float blendInFraction, float blendOutFraction, float fraction) {
    if (blendOutFraction > 0 && fraction >= (1-blendOutFraction)) {
      return 1-(fraction-(1-blendOutFraction))/blendOutFraction;
    } else if (blendInFraction > 0 && fraction <= blendInFraction) {
      return fraction/blendInFraction;
    } else {
      return 1;
    }
  }
}