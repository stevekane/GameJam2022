using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class PlayableAnimation {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
}

[Serializable]
public class AnimationJob : IEnumerator, IStoppable {
  public PlayableGraph Graph;
  public PlayableAnimation Animation;
  public AnimationDriver Driver;
  public AnimationLayerMixerPlayable Mixer;
  public AnimationClipPlayable Clip;
  public int EventHead { get; private set; }

  public AnimationJob(AnimationDriver driver, PlayableGraph graph, PlayableAnimation animation) {
    Graph = graph;
    Driver = driver;
    Animation = animation;
    Clip = AnimationClipPlayable.Create(Graph, Animation.Clip);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
  }

  public void Pause() => Clip.Pause();
  public void Resume() => Clip.Play();

  public void Start() {
    IsRunning = true;
    Clip.SetSpeed(Animation.Speed);
    Clip.SetDuration(Animation.Clip.length);
    Mixer.ConnectInput(1, Clip, 0, 1);
    Mixer.SetLayerMaskFromAvatarMask(1, Animation.Mask);
    Driver.Connect(this);
  }

  public void Stop() {
    IsRunning = false;
    Clip.Destroy();
    Mixer.Destroy();
    Driver.Disconnect(this);
  }

  public void Reset() {
    Stop();
    Start();
  }

  public object Current => EventHead;
  public bool IsRunning { get; private set; }
  public bool MoveNext() {
    if (IsRunning) {
      if (Clip.IsNull() || !Clip.IsValid() || Clip.IsDone()) {
        Stop();
      }
    }
    return IsRunning;
  }

  void UpdateEventHead() {
    var time = Clip.GetTime();
    var clip = Clip.GetAnimationClip();
    var frames = (clip.length*clip.frameRate+1)/* / Animator.speed*/;
    var interpolant = (float)(time/clip.length);
    EventHead = (int)Mathf.Lerp(0, frames, interpolant);
  }
}

[Serializable]
public class AnimationJobFacade : IEnumerator, IStoppable {
  AnimationJob Job;

  public AnimationJobFacade(AnimationJob job) {
    Job = job;
  }

  public void Reset() => Job.Reset();
  public void Stop() => Job.Stop();
  public bool MoveNext() => IsRunning;
  public bool IsRunning => Job.IsRunning;
  public object Current => Job.Current;
}

[RequireComponent(typeof(Animator))]
public class AnimationDriver : MonoBehaviour {
  Animator Animator;
  PlayableGraph Graph;
  AnimatorControllerPlayable AnimatorController;
  AnimationPlayableOutput Output;

  [NonSerialized] AnimationJob Job = null;

  void Awake() {
    Job = null;
    Animator = GetComponent<Animator>();
    Graph = PlayableGraph.Create("Animation Driver");
    Output = AnimationPlayableOutput.Create(Graph, "Animation Driver", Animator);
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Output.SetSourcePlayable(AnimatorController);
    Graph.Play();
  }
  void OnDestroy() => Graph.Destroy();

  void FixedUpdate() {
    if (Job != null && !Job.MoveNext()) {
      Job = null;
    }
  }

  public void Pause() => Job?.Pause();
  public void Resume() => Job?.Resume();

  public void Connect(AnimationJob animationJob) {
    animationJob.Mixer.ConnectInput(0, AnimatorController, 0, 1);
    Output.SetSourcePlayable(animationJob.Mixer);
  }

  public void Disconnect(AnimationJob animationJob) {
    Output.SetSourcePlayable(AnimatorController);
  }

  public AnimationJobFacade Play(PlayableAnimation animation) {
    Job?.Stop();
    Job = new AnimationJob(this, Graph, animation);
    Job.Start();
    return new AnimationJobFacade(Job);
  }
}