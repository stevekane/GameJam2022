using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.UI;

[Serializable]
public class AbilityParams {
  public AnimationClip WindupClip;
  public AnimationClip RecoveryClip;
  public AnimationClip StopClip;
  public Timeval BlendDuration = Timeval.FromMillis(500);
}

public class MyAbility : IEnumerator, IStoppable {
  public AbilityParams AbilityParams;
  public Bundle Bundle;
  public Bundle ParentBundle;
  public Animator Animator;
  public object Current { get => null; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() => Bundle.MoveNext();
  public bool IsRunning { get => Bundle.IsRunning; }

  public void Stop() {
    Bundle.Stop();
    ParentBundle.Run(OnStop);
  }
  public MyAbility(AbilityParams abilityParams, Animator animator, Bundle parentBundle) {
    AbilityParams = abilityParams;
    Animator = animator;
    ParentBundle = parentBundle;
    Bundle = new Bundle();
    Bundle.Run(Run);
  }
  public IEnumerator Run() {
    for (var i = 0; i < 10; i++) {
      yield return Animator.Run(AbilityParams.WindupClip);
      Debug.Log("PEW");
      yield return Animator.Run(AbilityParams.RecoveryClip);
    }
  }
  public IEnumerator OnStop() {
    for (var i = 0; i < 10; i++) {
      yield return Fiber.Wait(Timeval.FixedUpdatePerSecond);
      Debug.Log($"Burples {i}");
    }
  }
}

public class SequencePlayable : PlayableBehaviour {
  Playable Owner;
  PlayableGraph Graph;
  AnimationMixerPlayable Mixer;
  float Seeker;

  public override void OnPlayableCreate(Playable owner) {
    owner.SetInputWeight(0, 1);
    Owner = owner;
    Graph = Owner.GetGraph();
    Mixer = AnimationMixerPlayable.Create(Graph, 0);
    Graph.Connect(Mixer, 0, Owner, 0);
  }

  public void AddClip(AnimationClip clip) {
    var playableClip = AnimationClipPlayable.Create(Graph, clip);
    Owner.SetDuration(Owner.GetDuration()+clip.length);
    Mixer.SetInputCount(Mixer.GetInputCount()+1);
    Mixer.SetInputWeight(Mixer.GetInputCount()-1,0);
    Graph.Connect(playableClip, 0, Mixer, Mixer.GetInputCount()-1);
    playableClip.SetTime(0);
    playableClip.Pause();
  }

  public void SetSeeker(float v) => Seeker = v;

  public override void PrepareFrame(Playable playable, FrameData info) {
    var total = 0f;
    for (var i = 0; i < Mixer.GetInputCount(); i++) {
      Mixer.SetInputWeight(i, 0);
    }
    for (var i = 0; i < Mixer.GetInputCount(); i++) {
      var input = (AnimationClipPlayable)Mixer.GetInput(i);
      var clip = input.GetAnimationClip();
      var duration = clip.length;
      var withNextClip = total+duration;
      if (withNextClip >= Seeker) {
        var clipTime = Seeker-total;
        Debug.Log($"Should be playing {clip.name} at time {clipTime}");
        input.SetTime(clipTime);
        Mixer.SetInputWeight(i, 1);
        break;
      } else {
        total = withNextClip;
      }
    }
  }
}

public class AnimatorJobsTest : MonoBehaviour {
  public PlayableGraph Graph;
  public AnimationClipPlayable Clip1;
  public AnimationClipPlayable Clip2;
  public AnimationMixerPlayable Mixer;
  public Animator Animator;
  public AbilityParams AbilityParams;
  public Bundle Bundle = new Bundle();
  public Slider Clip1Slider;
  public Slider Clip2Slider;
  public Slider BlendSlider;

  public void Start() {
    var graph = PlayableGraph.Create();
    var output = AnimationPlayableOutput.Create(graph, "ToAnimator", Animator);
    var clip1 = AnimationClipPlayable.Create(graph, AbilityParams.WindupClip);
    var clip2 = AnimationClipPlayable.Create(graph, AbilityParams.RecoveryClip);
    var mixer = AnimationMixerPlayable.Create(graph, 2);
    graph.Connect(clip1, 0, mixer, 0);
    graph.Connect(clip2, 0, mixer, 1);
    output.SetSourcePlayable(mixer);
    clip1.Pause();
    clip2.Pause();
    graph.Play();
    Clip1 = clip1;
    Clip2 = clip2;
    Mixer = mixer;
    Graph = graph;
    Clip1Slider.minValue = 0;
    Clip1Slider.maxValue = Clip1.GetAnimationClip().length;
    Clip1Slider.onValueChanged.AddListener(SetClip1Time);
    Clip2Slider.minValue = 0;
    Clip2Slider.maxValue = Clip2.GetAnimationClip().length;
    Clip2Slider.onValueChanged.AddListener(SetClip2Time);
    BlendSlider.minValue = 0;
    BlendSlider.maxValue = 1;
    BlendSlider.onValueChanged.AddListener(SetBlend);
  }

  void SetClip1Time(float v) => Clip1.SetTime(v);
  void SetClip2Time(float v) => Clip2.SetTime(v);
  void SetBlend(float v) {
    Mixer.SetInputWeight(0, v);
    Mixer.SetInputWeight(1, 1-v);
  }

  [ContextMenu("Run Job")]
  public void RunJob() => Bundle.Run(new MyAbility(AbilityParams, Animator, Bundle));
  [ContextMenu("Stop")]
  public void Stop() {
    Bundle.Stop();
  }
  public void FixedUpdate() {
    Bundle.MoveNext();
  }
  public void OnDestroy() {
    Bundle.Stop();
    Graph.Destroy();
    Clip1Slider.onValueChanged.RemoveListener(SetClip1Time);
    Clip2Slider.onValueChanged.RemoveListener(SetClip2Time);
    BlendSlider.onValueChanged.RemoveListener(SetBlend);
  }
}