using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Traditional;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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
        Driver.SetInputWeight(this, BlendWeight());
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

[BurstCompile]
struct SampleLocalRotationJob : IAnimationJob {
  public ReadOnlyTransformHandle Handle;
  public NativeArray<quaternion> Value;
  public void ProcessRootMotion(AnimationStream stream) { }
  public void ProcessAnimation(AnimationStream stream) {
    Value[0] = Handle.GetLocalRotation(stream);
  }
}

[BurstCompile]
struct SpineRotationJob : IAnimationJob {
  public bool IsActive;
  public NativeArray<quaternion> LowerRoot;
  public NativeArray<quaternion> UpperRoot;
  public NativeArray<quaternion> LowerSpine;
  public NativeArray<quaternion> UpperSpine;
  public ReadWriteTransformHandle SpineHandle;
  public void ProcessRootMotion(AnimationStream stream) { }
  public void ProcessAnimation(AnimationStream stream) {
    if (!IsActive)
      return;
    var hipsLowerRotation = LowerRoot[0];
    var hipsUpperRotation = UpperRoot[0];
    var spineLowerRotation = LowerSpine[0];
    var spineUpperRotation = UpperSpine[0];
    spineUpperRotation = math.mul(hipsUpperRotation, spineUpperRotation);
    spineUpperRotation = math.mul(math.inverse(hipsLowerRotation), spineUpperRotation);
    spineUpperRotation = math.slerp(spineLowerRotation, spineUpperRotation, 1);
    SpineHandle.SetLocalRotation(stream, spineUpperRotation);
  }
}

public class AnimationDriver : MonoBehaviour {
  public Animator Animator;
  public float BaseSpeed { get; private set; }
  public double Speed => Animator.speed;
  PlayableGraph Graph;
  public AnimationLayerMixerPlayable Mixer;
  AnimatorControllerPlayable AnimatorController;
  AnimationScriptPlayable SpineCorrector;

  void Awake() {
#if UNITY_EDITOR
    if (!Animator) {
      Debug.LogError($"AnimationDriver must have Animator reference to initialize", this);
    }
#endif
    var hipsBone = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.Hips);
    var spineBone = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.Spine);
    Graph = PlayableGraph.Create("Animation Driver");
    var output = AnimationPlayableOutput.Create(Graph, "Animation Driver", Animator);
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 3);
    Mixer.ConnectInput(0, DualSampler(hipsBone, out LowerRoot, spineBone, out LowerSpine), 0, 1f);
    Mixer.ConnectInput(1, ScriptPlayable<NoopBehavior>.Create(Graph), 0, 1f);
    Mixer.ConnectInput(2, DualSampler(hipsBone, out UpperRoot, spineBone, out UpperSpine), 0, 1f);
    Mixer.GetInput(0).GetInput(0).AddInput(AnimatorController, 0, 1f);
    WholeBodySlot = new Slot { Input = Mixer.GetInput(1), MixerInputPort = 1 };
    UpperBodySlot = new Slot { Input = Mixer.GetInput(2).GetInput(0), MixerInputPort = 2 };
    Mixer.SetInputWeight(WholeBodySlot.MixerInputPort, 0f);
    Mixer.SetInputWeight(UpperBodySlot.MixerInputPort, 0f);
    Mixer.SetLayerMaskFromAvatarMask((uint)UpperBodySlot.MixerInputPort, Defaults.Instance.UpperBodyMask);
    SpineCorrector = NewSpineCorrector(spineBone);
    SpineCorrector.AddInput(Mixer, 0, 1f);
    output.SetSourcePlayable(SpineCorrector);
    Graph.Play();
    BaseSpeed = Animator.speed;
  }

  void OnDestroy() {
    LowerSpine.Dispose();
    LowerRoot.Dispose();
    UpperSpine.Dispose();
    UpperRoot.Dispose();
    Graph.Destroy();
  }

  public float TorsoRotation => 0f;

  public void SetSpeed(float speed) {
    Animator.SetSpeed(speed);
  }
  public void Pause() {
    Animator.SetSpeed(0);
  }
  public void Resume() {
    Animator.SetSpeed(BaseSpeed);
  }
  public void SetInputWeight(AnimationJob job, float weight) {
    var port = SlotForJob(job)?.MixerInputPort ?? 0;
    if (port > 0)
      Mixer.SetInputWeight(port, weight);
  }

  public void Connect(AnimationJob job) {
    Debug.Assert(job.Animation.Mask == Defaults.Instance.UpperBodyMask || job.Animation.Mask == null);
    var slot = job.Animation.Mask == Defaults.Instance.UpperBodyMask ? UpperBodySlot : WholeBodySlot;
    if (slot.CurrentJob != null)
      slot.CurrentJob.Stop();
    if (slot == UpperBodySlot) {
      var data = SpineCorrector.GetJobData<SpineRotationJob>();
      data.IsActive = true;
      SpineCorrector.SetJobData(data);
    }
    if (slot == WholeBodySlot) {
      Mixer.SetInputWeight(0, 0f);
    }
    slot.Input.AddInput(job.Clip, 0, 1f);
    Mixer.SetInputWeight(slot.MixerInputPort, 1f);
    slot.CurrentJob = job;
  }

  public void Disconnect(AnimationJob job) {
    if (SlotForJob(job) is var slot && slot != null) {
      Mixer.SetInputWeight(slot.MixerInputPort, 0f);
      slot.Input.DisconnectInput(0);
      slot.Input.SetInputCount(0);
      if (slot == UpperBodySlot) {
        var data = SpineCorrector.GetJobData<SpineRotationJob>();
        data.IsActive = false;
        SpineCorrector.SetJobData(data);
      }
      if (slot == WholeBodySlot) {
        Mixer.SetInputWeight(1, 1f);
      }
      slot.CurrentJob = null;
    }
  }

  public AnimationJob Play(TaskScope scope, AnimationJobConfig animation) {
    var job = new AnimationJob(this, Graph, animation);
    job.Start();
    _ = job.Run(scope);
    return job;
  }

  class Slot {
    public Playable Input;
    public AnimationJob CurrentJob;
    public int MixerInputPort;
  }
  Slot WholeBodySlot;
  Slot UpperBodySlot;
  Slot SlotForJob(AnimationJob job) {
    Slot port = 0 switch {
      _ when job == WholeBodySlot.CurrentJob => WholeBodySlot,
      _ when job == UpperBodySlot.CurrentJob => UpperBodySlot,
      _ => null,
    };
    return port;
  }

  NativeArray<quaternion> LowerRoot;
  NativeArray<quaternion> UpperRoot;
  NativeArray<quaternion> LowerSpine;
  NativeArray<quaternion> UpperSpine;

  AnimationScriptPlayable Sampler(Transform t, out NativeArray<quaternion> value) {
    value = new(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    var job = new SampleLocalRotationJob { Handle = ReadOnlyTransformHandle.Bind(Animator, t), Value = value };
    var playable = AnimationScriptPlayable.Create(Graph, job);
    return playable;
  }

  AnimationScriptPlayable DualSampler(Transform hip, out NativeArray<quaternion> hipRot, Transform spine, out NativeArray<quaternion> spineRot) {
    var hipSampler = Sampler(hip, out hipRot);
    var spineSampler = Sampler(spine, out spineRot);
    hipSampler.AddInput(spineSampler, 0, 1f);
    return hipSampler;
  }

  AnimationScriptPlayable NewSpineCorrector(Transform spine) {
    var job = new SpineRotationJob {
      IsActive = false,
      LowerRoot = LowerRoot,
      UpperRoot = UpperRoot,
      LowerSpine = LowerSpine,
      UpperSpine = UpperSpine,
      SpineHandle = ReadWriteTransformHandle.Bind(Animator, spine)
    };
    var playable = AnimationScriptPlayable.Create(Graph, job);
    return playable;
  }
}