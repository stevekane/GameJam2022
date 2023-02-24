using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Timeline;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace GodDammitSteveYouLilBitch {

#if UNITY_EDITOR
  using UnityEditor;
  using UnityEditor.UIElements;

  [CustomPropertyDrawer(typeof(AnimationTrackClip))]
  public class AnimationTrackClipPropertyDrawer : PropertyDrawer {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
      var container = new VisualElement();
      container.Add(new PropertyField(property.FindPropertyRelative("StartTime"), "Start Time"));
      container.Add(new PropertyField(property.FindPropertyRelative("EndTime"), "End Time"));
      container.Add(new PropertyField(property.FindPropertyRelative("AnimationClip"), "Clip"));
      return container;
    }
  }
  [CustomPropertyDrawer(typeof(AudioTrackClip))]
  public class AudioTrackClipPropertyDrawer : PropertyDrawer {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
      var container = new VisualElement();
      container.Add(new PropertyField(property.FindPropertyRelative("StartTime"), "Start Time"));
      container.Add(new PropertyField(property.FindPropertyRelative("EndTime"), "End Time"));
      container.Add(new PropertyField(property.FindPropertyRelative("AudioClip"), "Clip"));
      return container;
    }
  }
#endif

  [Serializable]
  public struct AnimationTrack {
    public AnimationCurve BlendCurve;
    public AnimationTrackClip[] TrackClips;
  }

  [Serializable]
  public struct AnimationTrackClip {
    public AnimationClip AnimationClip;
    public float StartTime;
    public float EndTime;
  }

  [Serializable]
  public struct AudioTrack {
    public AnimationCurve FadeCurve;
    public AudioTrackClip[] TrackClips;
  }

  [Serializable]
  public struct AudioTrackClip {
    public AudioClip AudioClip;
    public float StartTime;
    public float EndTime;
  }

  public class AudioTrackBehavior : PlayableBehaviour {
    Playable Playable;
    AudioMixerPlayable Mixer;
    AudioTrack AudioTrack;

    public float Weight { get; private set; }

    public void Initialize(AudioTrack audioTrack) {
      var graph = Playable.GetGraph();
      var duration = 0f;
      AudioTrack = audioTrack;
      Mixer = AudioMixerPlayable.Create(graph, 0);
      AudioTrack.TrackClips.ForEach(trackClip => {
        var clipPlayable = AudioClipPlayable.Create(graph, trackClip.AudioClip, looping: false);
        clipPlayable.Pause();
        Mixer.AddInput(clipPlayable, 0, 1);
        duration = Mathf.Max(duration, trackClip.EndTime);
      });
      Playable.ConnectInput(0, Mixer, 0, 1);
      Playable.SetDuration(duration);
    }

    public override void OnPlayableCreate(Playable playable) {
      Playable = playable;
    }

    public override void OnPlayableDestroy(Playable playable) {
      var inputCount = Mixer.GetInputCount();
      for (var i = 0; i < inputCount; i++) {
        var input = Mixer.GetInput(i);
        if (!input.IsNull()) {
          input.Destroy();
        }
      }
      Mixer.Destroy();
    }

    public override void PrepareFrame(Playable playable, FrameData info) {
      var time = (float)playable.GetTime();
      var duration = (float)playable.GetDuration();
      var inputCount = Mixer.GetInputCount();
      Weight = 0;
      for (var i = 0; i < inputCount; i++) {
        var start = AudioTrack.TrackClips[i].StartTime;
        var end = AudioTrack.TrackClips[i].EndTime;
        if (time >= start && time <= end) {
          var clipPlayable = Mixer.GetInput(i);
          var playState = clipPlayable.GetPlayState();
          if (playState == PlayState.Paused) {
            clipPlayable.SetTime(time-start);
            clipPlayable.Play();
          }
          Mixer.SetInputWeight(i, 1);
          Weight = AudioTrack.FadeCurve.Evaluate(Mathf.InverseLerp(start, end, time));
        } else {
          Mixer.SetInputWeight(i, 0);
        }
      }
    }
  }

  public class AnimationTrackBehavior : PlayableBehaviour {
    Playable Playable;
    AnimationMixerPlayable Mixer;
    AnimationTrack AnimationTrack;

    public float Weight { get; private set; }

    public void Initialize(AnimationTrack animationTrack) {
      var graph = Playable.GetGraph();
      var duration = 0f;
      AnimationTrack = animationTrack;
      Mixer = AnimationMixerPlayable.Create(graph, 0);
      AnimationTrack.TrackClips.ForEach(trackClip => {
        var clipPlayable = AnimationClipPlayable.Create(graph, trackClip.AnimationClip);
        clipPlayable.Pause();
        Mixer.AddInput(clipPlayable, 0, 0);
        duration = Mathf.Max(duration, trackClip.EndTime);
      });
      Playable.ConnectInput(0, Mixer, 0);
      Playable.SetDuration(duration);
    }

    public override void OnPlayableCreate(Playable playable) {
      Playable = playable;
    }

    public override void OnPlayableDestroy(Playable playable) {
      var inputCount = Mixer.GetInputCount();
      for (var i = 0; i < inputCount; i++) {
        var input = Mixer.GetInput(i);
        if (!input.IsNull()) {
          input.Destroy();
        }
      }
      Mixer.Destroy();
    }

    public override void PrepareFrame(Playable playable, FrameData info) {
      var time = (float)playable.GetTime();
      var duration = (float)playable.GetDuration();
      var inputCount = Mixer.GetInputCount();
      Weight = 0;
      for (var i = 0; i < inputCount; i++) {
        var start = AnimationTrack.TrackClips[i].StartTime;
        var end = AnimationTrack.TrackClips[i].EndTime;
        if (time >= start && time <= end) {
          var clipPlayable = Mixer.GetInput(i);
          clipPlayable.SetTime(time-start);
          Mixer.SetInputWeight(i, 1);
          Weight = AnimationTrack.BlendCurve.Evaluate(Mathf.InverseLerp(start, end, time));
        } else {
          Mixer.SetInputWeight(i, 0);
        }
      }
    }
  }

  public class PolymorphicGraph : MonoBehaviour {
    [SerializeField] AnimationClip BaseAnimationClip;
    [SerializeField] AnimationTrack AnimationTrack;
    [SerializeField] AudioTrack AudioTrack;
    [SerializeField] Animator Animator;
    [SerializeField] AudioSource AudioSource;
    [SerializeField] TimelineAsset TimelineAsset;

    PlayableGraph Graph;
    AnimationLayerMixerPlayable AnimationLayerMixer;
    ScriptPlayable<AnimationTrackBehavior> AnimationTrackPlayable;
    AnimationTrackBehavior AnimationTrackBehavior;
    AudioMixerPlayable AudioMixer;
    ScriptPlayable<AudioTrackBehavior> AudioTrackPlayable;
    AudioTrackBehavior AudioTrackBehavior;

    void Start() {
      Graph = PlayableGraph.Create("Polymorphic Graph");
      AnimationLayerMixer = AnimationLayerMixerPlayable.Create(Graph, 2);
      AnimationTrackPlayable = ScriptPlayable<AnimationTrackBehavior>.Create(Graph, 1);
      AnimationTrackBehavior = AnimationTrackPlayable.GetBehaviour();
      AudioMixer = AudioMixerPlayable.Create(Graph, 1);
      AudioTrackPlayable = ScriptPlayable<AudioTrackBehavior>.Create(Graph, 1);
      AudioTrackBehavior = AudioTrackPlayable.GetBehaviour();

      var animationBase = AnimationClipPlayable.Create(Graph, BaseAnimationClip);
      var animationOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
      AnimationLayerMixer.ConnectInput(0, animationBase, 0);
      AnimationLayerMixer.SetInputWeight(0, 1);
      AnimationLayerMixer.ConnectInput(1, AnimationTrackPlayable, 0);
      AnimationLayerMixer.SetInputWeight(1, 1);
      AnimationTrackBehavior.Initialize(AnimationTrack);
      // animationOutput.SetSourcePlayable(AnimationLayerMixer);

      var audioOutput = AudioPlayableOutput.Create(Graph, "Audio Output", AudioSource);
      AudioMixer.ConnectInput(0, AudioTrackPlayable, 0, 1);
      AudioTrackBehavior.Initialize(AudioTrack);
      // audioOutput.SetSourcePlayable(AudioMixer);

      var timelinePlayable = TimelineAsset.CreatePlayable(Graph, gameObject);
      var outputTrackCount = TimelineAsset.outputTrackCount;
      for (var i = 0; i < outputTrackCount; i++) {
        var track = TimelineAsset.GetOutputTrack(i);
        foreach (var output in track.outputs) {
          var targetType = output.outputTargetType;
          var playableOutput = targetType switch {
            _ when targetType == typeof(Animator) => AnimationPlayableOutput.Create(Graph, track.name, Animator),
            _ when targetType == typeof(AudioSource) => AudioPlayableOutput.Create(Graph, track.name, AudioSource),
            _ => PlayableOutput.Null
          };
          playableOutput.SetSourcePlayable(timelinePlayable, i);
          Debug.Log($"Created output {playableOutput} for track {track} because of targetType {targetType}");
        }
      }

      Graph.Play();
    }

    void Update() {
      AnimationLayerMixer.SetInputWeight(1, AnimationTrackBehavior.Weight);
      AudioMixer.SetInputWeight(0, AudioTrackBehavior.Weight);
    }

    void OnDestroy() {
      Graph.Destroy();
    }
  }
}