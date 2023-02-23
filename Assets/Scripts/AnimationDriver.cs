using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class AnimationJobConfig {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
  public Timeval[] PhaseDurations;
  [Range(0, 1)] public float BlendInFraction;
  [Range(0, 1)] public float BlendOutFraction;
}

[Serializable]
public struct TimelineBindings {
  public Collider Hitbox;
  public WeaponTrail WeaponTrail;
}

[Serializable]
public class TimelineTaskConfig {
  public TimelineAsset Asset;
  public TimelineBindings Bindings;
  public AvatarMask Mask = null;
  [Range(0, 1)] public float BlendInFraction;
  [Range(0, 1)] public float BlendOutFraction;
}

public interface IPlayableTask {
  public void Stop();
}

public class AnimationTask : IPlayableTask {
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

  public AnimationTask(AnimationDriver driver, PlayableGraph graph, AnimationJobConfig animation) {
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
    Driver.Disconnect(this);
    if (Clip.IsValid())
      Clip.Destroy();
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

public class TimelineTask : IPlayableTask {
  AnimationDriver Driver;
  internal TimelineTaskConfig Config;
  public Playable Playable { get; private set; }
  bool IsRunning => Playable.IsValid() && !Playable.IsDone();

  public TimelineTask(AnimationDriver driver, PlayableGraph graph, TimelineTaskConfig config) {
    Driver = driver;
    Config = config;

    Playable = config.Asset.CreatePlayable(graph, Driver.gameObject);
    Playable.SetOutputCount(config.Asset.outputTrackCount);
  }

  public void Start() {
    Playable.SetSpeed(Driver.Speed);
    Playable.SetDuration(Config.Asset.duration);
    Driver.Connect(this);
  }

  public void Stop() {
    Driver.Disconnect(this);
    if (Playable.IsValid())
      Playable.Destroy();  // TODO: destroy other things?
  }

  public TaskFunc WaitDone() => s => WaitDone(s);
  public async Task WaitDone(TaskScope scope) {
    while (IsRunning)
      await scope.Yield();
  }

  public async Task Run(TaskScope scope) {
    try {
      while (Playable.IsValid()) {
        if (Playable.IsDone())
          break;
        Driver.SetInputWeight(this, BlendWeight());
        Playable.SetSpeed(Driver.Speed);
        await scope.Yield();
      }
    } finally {
      Stop();
    }
  }

  float BlendWeight() {
    var fraction = Playable.GetTime()/Playable.GetDuration();
    // blending out
    if (this.Config.BlendOutFraction > 0 && fraction >= (1-Config.BlendOutFraction)) {
      return 1-(float)(fraction-(1-Config.BlendOutFraction))/Config.BlendOutFraction;
      // blending in
    } else if (Config.BlendInFraction > 0 && fraction <= Config.BlendInFraction) {
      return (float)fraction/Config.BlendInFraction;
      // full blending
    } else {
      return 1;
    }
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
  public float UpperWeight;
  public NativeArray<quaternion> LowerRoot;
  public NativeArray<quaternion> UpperRoot;
  public NativeArray<quaternion> LowerSpine;
  public NativeArray<quaternion> UpperSpine;
  public ReadWriteTransformHandle SpineHandle;
  public void ProcessRootMotion(AnimationStream stream) { }
  public void ProcessAnimation(AnimationStream stream) {
    //if (UpperWeight == 0f)
    //  return;
    var hipsLowerRotation = LowerRoot[0];
    var hipsUpperRotation = UpperRoot[0];
    var spineLowerRotation = LowerSpine[0];
    var spineUpperRotation = UpperSpine[0];
    spineUpperRotation = math.mul(hipsUpperRotation, spineUpperRotation);
    spineUpperRotation = math.mul(math.inverse(hipsLowerRotation), spineUpperRotation);
    spineUpperRotation = math.slerp(spineLowerRotation, spineUpperRotation, UpperWeight);
    SpineHandle.SetLocalRotation(stream, spineUpperRotation);
  }
}

[BurstCompile]
struct AnimationNoopJob : IAnimationJob {
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {}
}

public class AnimationDriver : MonoBehaviour {
  public Animator Animator;
  public AudioSource AudioSource;
  public float BaseSpeed { get; private set; }
  public double Speed => Animator.speed;
  PlayableGraph Graph;
  AnimationLayerMixerPlayable AnimationMixer;
  AudioMixerPlayable AudioMixer;
  AnimationScriptPlayable SpineCorrector;

  void Awake() {
#if UNITY_EDITOR
    if (!Animator) {
      Debug.LogError($"AnimationDriver must have Animator reference to initialize", this);
    }
#endif
    BaseSpeed = Animator.speed;

    var hipsBone = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.Hips);
    var spineBone = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.Spine);
    Graph = PlayableGraph.Create("Animation Driver");

    // Animation subgraph
    var animatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    AnimationMixer = AnimationLayerMixerPlayable.Create(Graph, 3);
    Graph.Connect(DualSampler(hipsBone, out LowerRoot, spineBone, out LowerSpine), 0, AnimationMixer, 0, 1f);
    Graph.Connect(AnimationScriptPlayable.Create(Graph, new AnimationNoopJob(), 1), 0, AnimationMixer, 1, 1f);
    Graph.Connect(DualSampler(hipsBone, out UpperRoot, spineBone, out UpperSpine), 0, AnimationMixer, 2, 1f);
    Graph.Connect(animatorController, 0, AnimationMixer.GetInput(0).GetInput(0), 0, 1f);
    Graph.Connect(AnimationMixer, 0, SpineCorrector = NewSpineCorrector(spineBone), 0, 1f);
    var animationOutput = AnimationPlayableOutput.Create(Graph, "Animation Driver Animations", Animator);
    animationOutput.SetSourcePlayable(SpineCorrector);

    // Audio subgraph
    AudioMixer = AudioMixerPlayable.Create(Graph, 2);
    Graph.Connect(AudioSlot(), 0, AudioMixer, 0, 1f);
    Graph.Connect(AudioSlot(), 0, AudioMixer, 1, 1f);
    var audioOutput = AudioPlayableOutput.Create(Graph, "Animation Driver Audio", AudioSource);
    audioOutput.SetSourcePlayable(AudioMixer);

    // Slots
    WholeBodySlot = new Slot { AnimationInput = AnimationMixer.GetInput(1), AudioInput = AudioMixer.GetInput(0) };
    UpperBodySlot = new Slot { AnimationInput = AnimationMixer.GetInput(2).GetInput(0), AudioInput = AudioMixer.GetInput(1) };
    AnimationMixer.SetLayerMaskFromAvatarMask(2, Defaults.Instance.UpperBodyMask);

    Graph.Play();
  }

  void OnDestroy() {
    LowerSpine.Dispose();
    LowerRoot.Dispose();
    UpperSpine.Dispose();
    UpperRoot.Dispose();
    Graph.Destroy();
  }

  public float TorsoRotation {
    get {
      var upperHips = Quaternion.Slerp(Quaternion.identity, UpperRoot[0], UpperBodySlot.InputWeight);
      var hipsForward = upperHips * Vector3.forward;
      var angle = Vector3.SignedAngle(hipsForward.XZ(), Vector3.forward, Vector3.up);
      return angle;
    }
  }

  public void SetSpeed(float speed) {
    Animator.SetSpeed(speed);
  }
  public void Pause() {
    Animator.SetSpeed(0);
  }
  public void Resume() {
    Animator.SetSpeed(BaseSpeed);
  }
  public void SetInputWeight(IPlayableTask task, float weight) {
    // TODO: set audio input weight?
    if (SlotForTask(task) is var slot && slot != null)
      slot.AnimationInput.SetInputWeight(0, weight);
  }

  public void Connect(AnimationTask task) {
    Debug.Assert(task.Animation.Mask == Defaults.Instance.UpperBodyMask || task.Animation.Mask == null);
    var slot = task.Animation.Mask == Defaults.Instance.UpperBodyMask ? UpperBodySlot : WholeBodySlot;
    if (slot.CurrentTask != null)
      slot.CurrentTask.Stop();
    Graph.Connect(task.Clip, 0, slot.AnimationInput, 0, 1f);
    slot.CurrentTask = task;
  }

  public void Connect(TimelineTask task) {
    var slot = UpperBodySlot;  // TODO
    if (slot.CurrentTask != null)
      slot.CurrentTask.Stop();

    var playable = task.Playable;
    var timeline = task.Config.Asset;
    for (int i = 0; i < timeline.outputTrackCount; i++) {
      var track = timeline.GetOutputTrack(i);
      //var trackType = track.GetType();

      //// Check for special track types and attach associated bindings
      //if (trackType == typeof(HitboxTrackAsset)) {
      //  // accessing inputs to check if sanity happens or not
      //  var input = playable.GetInput(i);
      //  var type = input.GetPlayableType();
      //  var trackPlayable = (ScriptPlayable<HitBoxTrackMixer>)input;
      //  var trackMixer = trackPlayable.GetBehaviour();
      //  // We probably need to create some kind of generic output for the graph.
      //  // I have no idea how this works and think should look at a graph generated
      //  // by playing a timeline via playable director for inspiration
      //  // TODO: Somehow, here we need to bind the track...whatever that even means
      //  // it's the "playerData" parameter that magically gets fed to the ProcessFrame call
      //  Debug.Log($"Found Hitbox track with mixer {input}:{type}");
      //} else if (trackType == typeof(WeaponTrailTrackAsset)) {
      //  var input = playable.GetInput(i);
      //  var type = input.GetPlayableType();
      //  Debug.Log($"Found WeaponTrail track with output {input}:{type}");
      //}

      foreach (var output in track.outputs) {
        if (output.outputTargetType == typeof(Animator)) {
          if (slot.AnimationInput.GetInput(0).IsNull())
            Graph.Connect(playable, i, slot.AnimationInput, 0, 1f);
        } else if (output.outputTargetType == typeof(AudioSource)) {
          if (slot.AudioInput.GetInput(0).IsNull()) {
            var noop = ScriptPlayable<NoopBehavior>.Create(Graph, 1);
            Graph.Connect(playable, i, noop, 0, 1f);
            Graph.Connect(noop, 0, slot.AudioInput, 0, 1f);
          }
        } else {
          var playableOutput = output.outputTargetType switch {
            Type type when type == typeof(Collider) => ObjectPlayableOutput(Graph, track.name, task.Config.Bindings.Hitbox),
            Type type when type == typeof(WeaponTrail) => ObjectPlayableOutput(Graph, track.name, task.Config.Bindings.WeaponTrail),
            _ => PlayableOutput.Null
          };
          if (!playableOutput.IsOutputNull())
            playableOutput.SetSourcePlayable(playable, i);
        }
      }
    }

    slot.CurrentTask = task;
  }

  public void Disconnect(IPlayableTask task) {
    if (SlotForTask(task) is var slot && slot != null) {
      // Delete all generated outputs (which for now is everything but animation and audio).
      var outputCount = Graph.GetOutputCount();
      var dynamicOutputs = Enumerable.Range(0, outputCount)
        .Select(i => Graph.GetOutput(i))
        .Where(o => !o.IsPlayableOutputOfType<AnimationPlayableOutput>() && !o.IsPlayableOutputOfType<AudioPlayableOutput>())
        .ToArray();
      foreach (var output in dynamicOutputs)
        Graph.DestroyOutput(output);

      slot.AnimationInput.DisconnectInput(0);
      slot.AudioInput.DisconnectInput(0);
      slot.CurrentTask = null;
    }
  }

  public AnimationTask Play(TaskScope scope, AnimationJobConfig animation) {
    var task = new AnimationTask(this, Graph, animation);
    task.Start();
    _ = task.Run(scope);
    return task;
  }

  public TimelineTask PlayTimeline(TaskScope scope, TimelineTaskConfig config) {
    var task = new TimelineTask(this, Graph, config);
    task.Start();
    _ = task.Run(scope);
    return task;
  }

  void Update() {
    var data = SpineCorrector.GetJobData<SpineRotationJob>();
    data.UpperWeight = UpperBodySlot.InputWeight;
    SpineCorrector.SetJobData(data);
    // Update mixer input weights based on the slot state. Could do this differently, but this is the cleanest IMO.
    AnimationMixer.SetInputWeight(1, WholeBodySlot.InputWeight);
    AnimationMixer.SetInputWeight(2, UpperBodySlot.InputWeight);
  }

  class Slot {
    public Playable AnimationInput;
    public Playable AudioInput;
    public IPlayableTask CurrentTask;
    public float InputWeight => AnimationInput.GetInput(0).IsNull() ? 0f : AnimationInput.GetInputWeight(0);
  }
  Slot WholeBodySlot;
  Slot UpperBodySlot;
  Slot SlotForTask(IPlayableTask task) {
    Slot port = 0 switch {
      _ when task == WholeBodySlot.CurrentTask => WholeBodySlot,
      _ when task == UpperBodySlot.CurrentTask => UpperBodySlot,
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
    value[0] = quaternion.identity;
    var job = new SampleLocalRotationJob { Handle = ReadOnlyTransformHandle.Bind(Animator, t), Value = value };
    var playable = AnimationScriptPlayable.Create(Graph, job, 1);
    return playable;
  }

  AnimationScriptPlayable DualSampler(Transform hip, out NativeArray<quaternion> hipRot, Transform spine, out NativeArray<quaternion> spineRot) {
    var hipSampler = Sampler(hip, out hipRot);
    var spineSampler = Sampler(spine, out spineRot);
    Graph.Connect(spineSampler, 0, hipSampler, 0, 1f);
    return hipSampler;
  }

  Playable AudioSlot() {
    var mixer = AudioMixerPlayable.Create(Graph, 1);
    return mixer;
  }

  PlayableOutput ObjectPlayableOutput(PlayableGraph graph, string name, UnityEngine.Object obj) {
    var output = ScriptPlayableOutput.Create(graph, name);
    output.SetUserData(obj);
    output.SetReferenceObject(obj);
    return output;
  }

  AnimationScriptPlayable NewSpineCorrector(Transform spine) {
    var job = new SpineRotationJob {
      UpperWeight = 0f,
      LowerRoot = LowerRoot,
      UpperRoot = UpperRoot,
      LowerSpine = LowerSpine,
      UpperSpine = UpperSpine,
      SpineHandle = ReadWriteTransformHandle.Bind(Animator, spine)
    };
    var playable = AnimationScriptPlayable.Create(Graph, job, 1);
    return playable;
  }
}