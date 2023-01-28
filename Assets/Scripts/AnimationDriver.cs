using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
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

[Serializable]
public struct MixerJobData {
  public NativeArray<ReadWriteTransformHandle> BoneHandles;
  public NativeArray<int> BoneParents;
  public NativeArray<NativeArray<bool>> BoneActivesPerLayer;
  public bool ReverseBaseLayerRotation;
  public bool MeshSpaceRotations;

  AvatarMaskBodyPart[] BoneBodyParts;  // Main thread only.
  public void Init(Animator animator) {
    var transforms = animator.avatarRoot.GetComponentsInChildren<Transform>();
    BoneHandles = new(transforms.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    BoneParents = new(transforms.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    for (int i = 0; i < transforms.Length; i++) {
      BoneHandles[i] = ReadWriteTransformHandle.Bind(animator, transforms[i]);
      BoneParents[i] = Array.FindIndex(transforms, t => t == transforms[i].parent);
    }
    BoneActivesPerLayer = new(0, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

    var self = this;
    BoneBodyParts = transforms.Select(t => self.GetAvatarMaskBodyPart(animator.avatarRoot, animator.avatar, t)).ToArray();
  }

  public void Dispose() {
    BoneHandles.Dispose();
    BoneParents.Dispose();
    BoneActivesPerLayer.Dispose();
  }

  // Adds a new layer to our BoneMaskPerLayer cache, and turns its bones on/off depending on whether they
  // are enabled by `mask` - defaults to ON if `mask` is null.
  public void SetLayerMaskFromAvatarMask(int layer, AvatarMask mask) {
    var old = BoneActivesPerLayer;
    BoneActivesPerLayer = new(layer+1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    NativeArray<NativeArray<bool>>.Copy(old, BoneActivesPerLayer, layer);
    var boneActives = BoneActivesPerLayer[layer] = new(BoneBodyParts.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    for (int bone = 0; bone < BoneBodyParts.Length; bone++)
      boneActives[bone] = mask ? mask.GetHumanoidBodyPartActive(BoneBodyParts[bone]) : true;
    old.Dispose();
  }

  static Dictionary<string, AvatarMaskBodyPart> HumanNameToBodyPart = new() {
    {"Root", AvatarMaskBodyPart.Root },
    {"Spine", AvatarMaskBodyPart.Body },
    {"Head", AvatarMaskBodyPart.Head },
    {"LeftUpperLeg", AvatarMaskBodyPart.LeftLeg },
    {"RightUpperLeg", AvatarMaskBodyPart.RightLeg },
    {"LeftShoulder", AvatarMaskBodyPart.LeftArm },
    {"RightShoulder", AvatarMaskBodyPart.RightArm },
    //{"LeftHand", AvatarMaskBodyPart.LeftFingers }, // fuck fingers
    //{"RightHand", AvatarMaskBodyPart.RightFingers },
    {"LeftFoot", AvatarMaskBodyPart.LeftFootIK },
    {"RightFoot", AvatarMaskBodyPart.RightFootIK },
    {"LeftHand", AvatarMaskBodyPart.LeftHandIK },
    {"RightHand", AvatarMaskBodyPart.RightHandIK },
  };
  // Returns the AvatarMaskBodyPart for the region of the avatar tree given by `t`, which must be a descendent
  // of avatarRoot.
  AvatarMaskBodyPart GetAvatarMaskBodyPart(Transform avatarRoot, Avatar avatar, Transform t) {
    if (avatar == null || t == avatarRoot)
      return AvatarMaskBodyPart.Root;
    var hb = avatar.humanDescription.human.FirstOrDefault(hb => hb.boneName == t.name);
    if (hb.boneName == t.name && HumanNameToBodyPart.TryGetValue(hb.humanName, out var bodyPart))
      return bodyPart;
    return GetAvatarMaskBodyPart(avatarRoot, avatar, t.parent);
  }
}

public struct MixerJob : IAnimationJob {
  public MixerJobData Data;
  public void ProcessRootMotion(AnimationStream stream) {
    // I don't know what this shit is used for.
    //stream.velocity = topStream.velocity;
    //stream.angularVelocity = topStream.angularVelocity;
  }
  public void ProcessAnimation(AnimationStream stream) {
    var baseStream = stream.GetInputStream(0);
    for (var i = 0; i < Data.BoneHandles.Length; i++) {
      var handle = Data.BoneHandles[i];
      int layer = FindInputStreamForBone(stream, i);
      var inStream = stream.GetInputStream(layer);
      handle.SetLocalPosition(stream, handle.GetLocalPosition(inStream));
      handle.SetLocalScale(stream, handle.GetLocalScale(inStream));
      var boneIsTopmostActive = Data.BoneActivesPerLayer[layer][i] && Data.BoneParents[i] >= 0 && !Data.BoneActivesPerLayer[layer][Data.BoneParents[i]];
      if (boneIsTopmostActive) {
        // If our parent bone is disabled in this layer, do some special shit to try to preserve the animation's intent for this bone.
        // First, use a "mesh-space" rotation - figure out the total rotation that WOULD be imparted on this bone and apply it locally.
        Quaternion rotation = Data.MeshSpaceRotations ?
          Quaternion.Inverse(RootRotation(inStream)) * handle.GetRotation(inStream) :
          handle.GetLocalRotation(inStream);
        if (Data.ReverseBaseLayerRotation) {
          // Next, reverse the rotation that our base layer applies to our parent bone.
          var parentHandle = Data.BoneHandles[Data.BoneParents[i]];
          var baseParentRotation = Quaternion.Inverse(RootRotation(baseStream)) * parentHandle.GetRotation(baseStream);
          rotation = Quaternion.Inverse(baseParentRotation) * rotation;
        }
        handle.SetLocalRotation(stream, rotation);
      } else {
        handle.SetLocalRotation(stream, handle.GetLocalRotation(inStream));
      }
    }
  }
  Quaternion RootRotation(AnimationStream stream) => Data.BoneHandles[0].GetRotation(stream);
  int FindInputStreamForBone(AnimationStream stream, int boneIdx) {
    for (var layer = stream.inputStreamCount-1; layer >= 0; layer--) {
      if (Data.BoneActivesPerLayer[layer][boneIdx] && stream.GetInputStream(layer).isValid)
        return layer;
    }
    return 0;  // default to top layer
  }
}

public class AnimationDriver : MonoBehaviour {
  public Animator Animator;
  public float BaseSpeed { get; private set; }
  public double Speed => Animator.speed;
  PlayableGraph Graph;
  AnimatorControllerPlayable AnimatorController;
  public AnimationScriptPlayable Mixer;
  [SerializeField] MixerJobData MixerJobData = new();
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
    MixerJobData.Init(Animator);
    MixerJobData.SetLayerMaskFromAvatarMask(0, null);
    Mixer = AnimationScriptPlayable.Create(Graph, new MixerJob() { Data = MixerJobData });
    Mixer.SetProcessInputs(false);
    Mixer.AddInput(AnimatorController, 0, 1f);
    Output.SetSourcePlayable(Mixer);
    Graph.Play();
    BaseSpeed = Animator.speed;
  }

  void OnDestroy() {
    Graph.Destroy();
    MixerJobData.Dispose();
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

  public void Connect(AnimationJob job) {
    job.InputPort = Mixer.AddInput(job.Clip, 0, 1f);
    SetLayerMaskFromAvatarMask(job.InputPort, job.Animation.Mask);
    Jobs.Add(job);
  }

  public void Disconnect(AnimationJob job) {
    if (Jobs.Remove(job))
      Mixer.DisconnectInput(job.InputPort);
    if (Jobs.Count == 0) {
      Mixer.SetInputCount(1);
      SetLayerMaskFromAvatarMask(0, null);  // reset BoneMaskPerLayer
    }
  }

  public AnimationJob Play(TaskScope scope, AnimationJobConfig animation) {
    var job = new AnimationJob(this, Graph, animation);
    job.Start();
    _ = job.Run(scope);
    return job;
  }

  void SetLayerMaskFromAvatarMask(int layer, AvatarMask mask) {
    Debug.Assert(layer+1 == Mixer.GetInputCount(), "Oops I assumed we would only add masks to new layers");
    MixerJobData.SetLayerMaskFromAvatarMask(layer, mask);
    var job = Mixer.GetJobData<MixerJob>();
    job.Data = MixerJobData;
    Mixer.SetJobData(job);
  }
}