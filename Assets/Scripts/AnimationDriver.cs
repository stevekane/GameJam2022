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
  [Range(0,1)]
  public float BlendInFraction;
  [Range(0,1)]
  public float BlendOutFraction;
}

public class AnimationJob {
  PlayableGraph Graph;
  AnimationJobConfig Animation;
  AnimationDriver Driver;
  public AnimationLayerMixerPlayable Mixer;
  AnimationClipPlayable Clip;
  double DesiredSpeed = 1;
  public bool IsRunning { get; private set; } = false;
  public int CurrentFrame { get; private set; } = -1;
  public int NumFrames => (int)(Clip.GetAnimationClip() is var clip ? clip.length*clip.frameRate+1 : 0);

  public AnimationJob(AnimationDriver driver, PlayableGraph graph, AnimationJobConfig animation) {
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

  public TaskFunc WaitFrame(int frame) => s => WaitFrame(s, frame);
  public async Task WaitFrame(TaskScope scope, int frame) {
    while (CurrentFrame < frame && IsRunning)
      await scope.Yield();  // tick or yield?
  }
  public TaskFunc PauseAtFrame(int frame) => s => PauseAtFrame(s, frame);
  public async Task PauseAtFrame(TaskScope scope, int frame) {
    while (CurrentFrame < frame && IsRunning)
      await scope.Yield();  // tick or yield?
    Pause();
  }
  public TaskFunc WaitDone() => s => WaitDone(s);
  public async Task WaitDone(TaskScope scope) {
    while (IsRunning)
      await scope.Yield();
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
        Mixer.SetInputWeight(1, BlendWeight());
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

public class AnimationDriver : MonoBehaviour {
  public Animator Animator;
  public float BaseSpeed { get; private set; }
  public double speed => Animator.speed;
  PlayableGraph Graph;
  AnimatorControllerPlayable AnimatorController;
  AnimationPlayableOutput Output;

  AnimationJob Job = null;

  void Awake() {
    #if UNITY_EDITOR
    if (!Animator) {
      Debug.LogError($"AnimationDriver must have Animator reference to initialize", this);
    }
    #endif
    Graph = PlayableGraph.Create("Animation Driver");
    Output = AnimationPlayableOutput.Create(Graph, "Animation Driver", Animator);
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Output.SetSourcePlayable(AnimatorController);
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
    job.Mixer.ConnectInput(0, AnimatorController, 0, 1);
    Output.SetSourcePlayable(job.Mixer);
  }

  public void Disconnect(AnimationJob job) {
    if (job == Job)  // Ignore Disconnect from inactive jobs.
      Output.SetSourcePlayable(AnimatorController);
  }

  public AnimationJob Play(TaskScope scope, AnimationClip clip) => Play(scope, new AnimationJobConfig() { Clip = clip });
  public AnimationJob Play(TaskScope scope, AnimationJobConfig animation) {
    Job?.Stop();
    var job = new AnimationJob(this, Graph, animation);
    Job = job;
    _ = Run(scope);
    return job;
  }

  async Task Run(TaskScope scope) {
    try {
      Job.Start();
      await Job.Run(scope);
    } finally {
      Job = null;
    }
  }
}