using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class AnimationJobConfig {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
  public Timeval[] PhaseDurations;
  [Range(0,1)]
  public float BlendInFraction;
  [Range(0,1)]
  public float BlendOutFraction;
}

public class AnimationJob {
  PlayableGraph Graph;
  public AnimationJobConfig Animation;
  AnimationDriver Driver;
  public int InputPort = -1;
  public AnimationClipPlayable Clip;
  double DesiredSpeed = 1;
  public bool IsRunning { get; private set; } = false;
  public int CurrentFrame { get; private set; } = -1;
  public int CurrentPhase { get; private set; } = 0;
  public int NumFrames => (int)(Animation.Clip.length*Animation.Clip.frameRate+1);
  public AnimationEvent[] Phases => Animation.Clip.events;
  public int NumPhases => Phases.Length+1;

  public AnimationJob(AnimationDriver driver, PlayableGraph graph, AnimationJobConfig animation) {
    Graph = graph;
    Driver = driver;
    Animation = animation;
    Clip = AnimationClipPlayable.Create(Graph, Animation.Clip);

    // TODO: Legacy. Update all old anims and remove this.
    if (Animation.PhaseDurations.Length == 0 && NumPhases == 1) {
      Animation.PhaseDurations = new[] { Timeval.FromSeconds(Animation.Clip.length) };
    }

    Debug.Assert(Animation.PhaseDurations.Length == NumPhases,
      $"Number of phase durations should match the number of phases (animation events + 1) for clip {animation.Clip}");
    SetPhaseDuration();
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

  public TaskFunc WaitPhase(int phase) => s => WaitPhase(s, phase);
  public async Task WaitPhase(TaskScope scope, int phase) {
    var time = GetPhaseEndTime(phase);
    while (IsRunning && Clip.GetTime() < time)
      await scope.Yield();
  }
  public TaskFunc PauseAfterPhase(int phase) => s => PauseAfterPhase(s, phase);
  public async Task PauseAfterPhase(TaskScope scope, int phase) {
    await WaitPhase(scope, phase);
    Pause();
  }

  public TaskFunc WaitFrame(int frame) => s => WaitFrame(s, frame);
  public async Task WaitFrame(TaskScope scope, int frame) {
    while (CurrentFrame < frame && IsRunning)
      await scope.Yield();
  }
  public TaskFunc WaitDone() => s => WaitDone(s);
  public async Task WaitDone(TaskScope scope) {
    while (IsRunning)
      await scope.Yield();
  }

  // TDO: condense start/run?
  public void Start() {
    Clip.SetSpeed(DesiredSpeed * Animation.Speed * Driver.Speed);
    Clip.SetDuration(Animation.Clip.length);
    Driver.Connect(this);
    IsRunning = true;
  }

  public void Stop() {
    IsRunning = false;
    if (Clip.IsValid())
      Clip.Destroy();
    Driver.Disconnect(this);
  }

  float BlendWeight() {
    var time = Clip.GetTime();
    var duration = Clip.GetDuration();
    var fraction = time/duration;
    // blending out
    if (Animation.BlendOutFraction > 0 && fraction >= (1-Animation.BlendOutFraction)) {
      return 1-(float)(fraction-(1-Animation.BlendOutFraction))/Animation.BlendOutFraction;
    // blending in
    } else if (Animation.BlendInFraction > 0 && fraction <= Animation.BlendInFraction) {
      return (float)fraction/Animation.BlendInFraction;
    // full blending
    } else {
      return 1;
    }
  }

  public async Task Run(TaskScope scope) {
    try {
      while (IsRunning && Clip.IsValid()) {
        UpdateCurrentFrame();
        UpdateCurrentPhase();
        Driver.Mixer.SetInputWeight(InputPort, BlendWeight());
        Clip.SetSpeed(DesiredSpeed * Animation.Speed * Driver.Speed);
        if (Clip.IsDone())
          break;
        await scope.Yield();
      }
    } finally {
      // TODO: how to pause at end?
      Stop();
    }
  }

  void UpdateCurrentFrame() {
    var clip = Clip.GetAnimationClip();
    var frames = (clip.length*clip.frameRate+1)/* / Animator.speed*/;
    var interpolant = (float)(Clip.GetTime()/clip.length);
    CurrentFrame = (int)Mathf.Lerp(0, frames, interpolant);
  }
  // TODO: This doesn't support looping.
  void UpdateCurrentPhase() {
    var time = Clip.GetTime();
    var oldPhase = CurrentPhase;
    while (CurrentPhase < Phases.Length && time >= Phases[CurrentPhase].time)
      CurrentPhase++;
    if (oldPhase != CurrentPhase)
      SetPhaseDuration();
  }

  float GetPhaseStartTime(int phase) => phase-1 < 0 ? 0 : Phases[phase-1].time;
  float GetPhaseEndTime(int phase) => phase < Phases.Length ? Phases[phase].time : Animation.Clip.length;
  void SetPhaseDuration() {
    var duration = Animation.PhaseDurations[CurrentPhase];
    var clipDuration = GetPhaseEndTime(CurrentPhase) - GetPhaseStartTime(CurrentPhase);
    DesiredSpeed = duration.Seconds == 0f ? float.MaxValue : clipDuration/duration.Seconds;
  }
}

public class AnimationDriver : MonoBehaviour {
  public Animator Animator;
  public float BaseSpeed { get; private set; }
  public double Speed => Animator.speed;
  PlayableGraph Graph;
  AnimatorControllerPlayable AnimatorController;
  public AnimationLayerMixerPlayable Mixer;
  AnimationPlayableOutput Output;

  List<AnimationJob> Jobs = new();

  void Awake() {
    #if UNITY_EDITOR
    if (!Animator) {
      Debug.LogError($"AnimationDriver must have Animator reference to initialize", this);
    }
    #endif
    Graph = PlayableGraph.Create("Animation Driver");
    Output = AnimationPlayableOutput.Create(Graph, "Animation Driver", Animator);
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
    Mixer.ConnectInput(0, AnimatorController, 0, 1);
    Output.SetSourcePlayable(Mixer);
    Graph.Play();
    BaseSpeed = Animator.speed;
  }

  void OnDestroy() => Graph.Destroy();

  public void SetSpeed(float speed) {
    Animator.SetSpeed(speed);
  }

  public void Pause() {
    Animator.SetSpeed(0);
  }

  public void Resume() {
    Animator.SetSpeed(BaseSpeed);
  }

  public void Connect(AnimationJob job) {
    job.InputPort = Mixer.AddInput(job.Clip, 0, 1);
    if (job.Animation.Mask)  // is InputPort the right arg here?
      Mixer.SetLayerMaskFromAvatarMask((uint)job.InputPort, job.Animation.Mask);
    Jobs.Add(job);
  }

  public void Disconnect(AnimationJob job) {
    if (Jobs.Remove(job))
      Mixer.DisconnectInput(job.InputPort);
    if (Jobs.Count == 0)
      Mixer.SetInputCount(1);
  }

  public AnimationJob Play(TaskScope scope, AnimationJobConfig animation) {
    var job = new AnimationJob(this, Graph, animation);
    job.Start();
    _ = job.Run(scope);
    return job;
  }

  async Task Run(TaskScope scope, AnimationJob job) {
  }
}