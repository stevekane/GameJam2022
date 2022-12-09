using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class AnimationJobConfig {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
}

// TODO: rename
public class AnimationJobTask {
  PlayableGraph Graph;
  AnimationJobConfig Animation;
  AnimationDriver Driver;
  public AnimationLayerMixerPlayable Mixer;
  AnimationClipPlayable Clip;
  double DesiredSpeed = 1;
  bool IsRunning = false;
  public int CurrentFrame { get; private set; } = -1;

  public AnimationJobTask(AnimationDriver driver, PlayableGraph graph, AnimationJobConfig animation) {
    Graph = graph;
    Driver = driver;
    Animation = animation;
    Clip = AnimationClipPlayable.Create(Graph, Animation.Clip);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
  }

  public void SetSpeed(double speed) => DesiredSpeed = speed;

  public void Pause() {
    if (Clip.IsValid())
      Clip.Pause();
  }
  public void Resume() {
    if (Clip.IsValid())
      Clip.Play();
  }

  public async Task WaitFrame(TaskScope scope, int frame) {
    while (CurrentFrame < frame && IsRunning)
      await scope.Yield();  // tick or yield?
  }

  public async Task WaitDone(TaskScope scope) {
    while (IsRunning)
      await scope.Yield();
      // await scope.Tick();
  }

  // TDO: condense start/run?
  public void Start() {
    Clip.SetSpeed(DesiredSpeed * Animation.Speed * Driver.speed);
    Clip.SetDuration(Animation.Clip.length);
    Mixer.ConnectInput(1, Clip, 0, 1);
    if (Animation.Mask)
      Mixer.SetLayerMaskFromAvatarMask(1, Animation.Mask);
    Driver.Connect(this);
    IsRunning = true;
  }

  public void Stop() {
    IsRunning = false;
    if (Clip.IsValid())
      Clip.Destroy();
    if (Mixer.IsValid())
      Mixer.Destroy();
    Driver.Disconnect(this);
  }

  public async Task Run(TaskScope scope) {
    try {
      while (IsRunning && Clip.IsValid()) {
        Clip.SetSpeed(DesiredSpeed * Animation.Speed * Driver.speed);
        UpdateCurrentFrame();
        if (Clip.IsDone())
          break;
        // await scope.Tick();
        await scope.Yield(); // tick or yield?
      }
    } finally {
      Stop();
    }
  }

  void UpdateCurrentFrame() {
    var clip = Clip.GetAnimationClip();
    var frames = (clip.length*clip.frameRate+1)/* / Animator.speed*/;
    var interpolant = (float)(Clip.GetTime()/clip.length);
    CurrentFrame = (int)Mathf.Lerp(0, frames, interpolant);
  }
}

[Serializable]
public class AnimationJob : IEnumerator, IStoppable {
  public PlayableGraph Graph;
  public AnimationJobConfig Animation;
  public AnimationDriver Driver;
  public AnimationLayerMixerPlayable Mixer;
  public AnimationClipPlayable Clip;
  public double BaseSpeed { get; private set; }
  public int EventHead { get; private set; } = -1;
  public EventSource<int> OnFrame = new();

  public AnimationJob(AnimationDriver driver, PlayableGraph graph, AnimationJobConfig animation) {
    Graph = graph;
    Driver = driver;
    Animation = animation;
    Clip = AnimationClipPlayable.Create(Graph, Animation.Clip);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
    BaseSpeed = Clip.GetSpeed();
  }

  public void Pause() {
    if (Clip.IsValid() && !Clip.IsNull()) {
      Clip.Pause();
    }
  }
  public void Resume() {
    if (Clip.IsValid() && !Clip.IsNull()) {
      Clip.Play();
    }
  }

  public void SetSpeed(float speed) {
    if (Clip.IsValid() && !Clip.IsNull()) {
      Clip.SetSpeed(speed);
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
    if (!Clip.IsNull() && Clip.IsValid()) {
      Clip.Destroy();
    }
    if (!Mixer.IsNull() && Mixer.IsValid()) {
      Mixer.Destroy();
    }
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
      Clip.SetSpeed(BaseSpeed*Animation.Speed*Driver.speed);
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

public class AnimationDriver : MonoBehaviour {
  public Animator Animator;
  public float BaseSpeed { get; private set; }
  public double speed => Animator.speed;
  PlayableGraph Graph;
  AnimatorControllerPlayable AnimatorController;
  AnimationPlayableOutput Output;

  AnimationJob Job = null;
  AnimationJobTask Task = null;

  void Awake() {
    Graph = PlayableGraph.Create("Animation Driver");
    Output = AnimationPlayableOutput.Create(Graph, "Animation Driver", Animator);
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Output.SetSourcePlayable(AnimatorController);
    Graph.Play();
    BaseSpeed = Animator.speed;
  }

  void OnDestroy() => Graph.Destroy();

  void FixedUpdate() {
    if (Job != null && !Job.MoveNext()) {
      Job = null;
    }
  }

  public void SetSpeed(float speed) {
    Animator.SetSpeed(speed);
  }

  public void Pause() {
    Job?.Pause();
    Animator.SetSpeed(0);
  }

  public void Resume() {
    Job?.Resume();
    Animator.SetSpeed(BaseSpeed);
  }

  public void Connect(AnimationJob animationJob) {
    animationJob.Mixer.ConnectInput(0, AnimatorController, 0, 1);
    Output.SetSourcePlayable(animationJob.Mixer);
  }

  public void Disconnect(AnimationJob animationJob) {
    Output.SetSourcePlayable(AnimatorController);
  }

  public void Connect(AnimationJobTask animationJob) {
    animationJob.Mixer.ConnectInput(0, AnimatorController, 0, 1);
    Output.SetSourcePlayable(animationJob.Mixer);
  }

  public void Disconnect(AnimationJobTask animationJob) {
    Output.SetSourcePlayable(AnimatorController);
  }

  public AnimationJobFacade Play(AnimationJobConfig animation) {
    Job?.Stop();
    Job = new AnimationJob(this, Graph, animation);
    Job.Start();
    return new AnimationJobFacade(Job);
  }

  public AnimationJobTask Play(TaskScope scope, AnimationClip clip) => Play(scope, new AnimationJobConfig() { Clip = clip });
  public AnimationJobTask Play(TaskScope scope, AnimationJobConfig animation) {
    Task?.Stop();
    var task = new AnimationJobTask(this, Graph, animation);
    Task = task;
    _ = Run(scope);
    return task;
  }

  async Task Run(TaskScope scope) {
    try {
      Task.Start();
      await Task.Run(scope);
    } finally {
      Task = null;
    }
  }
}