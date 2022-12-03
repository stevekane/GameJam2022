using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class AnimationJobConfig {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
}

[Serializable]
public class AnimationJob : IEnumerator, IStoppable {
  public PlayableGraph Graph;
  public AnimationJobConfig Animation;
  public AnimationDriver Driver;
  public AnimationLayerMixerPlayable Mixer;
  public AnimationClipPlayable Clip;
  public int EventHead { get; private set; } = -1;
  public EventSource<int> OnFrame = new();

  public AnimationJob(AnimationDriver driver, PlayableGraph graph, AnimationJobConfig animation) {
    Graph = graph;
    Driver = driver;
    Animation = animation;
    Clip = AnimationClipPlayable.Create(Graph, Animation.Clip);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
  }

  public void Pause() {
    if (Clip.IsValid() && Clip.IsNull() && Clip.GetPlayState() == PlayState.Playing) {
      Clip.Pause();
    }
  }
  public void Resume() {
    if (Clip.IsValid() && !Clip.IsNull() && Clip.GetPlayState() == PlayState.Paused) {
      Clip.Play();
    }
  }

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
      if (Clip.IsNull() || !Clip.IsValid()) {
        Stop();
      } else {
        UpdateEventHead();
        if (Clip.IsDone()) {
          Stop();
        }
      }
    }
    return IsRunning;
  }

  void UpdateEventHead() {
    var time = Clip.GetTime();
    var clip = Clip.GetAnimationClip();
    var frames = (clip.length*clip.frameRate+1)/* / Animator.speed*/;
    var interpolant = (float)(time/clip.length);
    var eventHead = (int)Mathf.Lerp(0, frames, interpolant);
    if (eventHead > EventHead) {
      for (var i = EventHead+1; i <= eventHead; i++) {
        OnFrame.Fire(i);
      }
    }
    EventHead = eventHead;
  }
}

[Serializable]
public class AnimationJobFacade : IEnumerator, IStoppable {
  AnimationJob Job;

  public AnimationJobFacade(AnimationJob job) {
    Job = job;
  }

  public EventSource<int> OnFrame => Job.OnFrame;
  public void Pause() => Job.Pause();
  public void Resume() => Job.Resume();
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

  public AnimationJobFacade Play(AnimationJobConfig animation) {
    Job?.Stop();
    Job = new AnimationJob(this, Graph, animation);
    Job.Start();
    return new AnimationJobFacade(Job);
  }
}