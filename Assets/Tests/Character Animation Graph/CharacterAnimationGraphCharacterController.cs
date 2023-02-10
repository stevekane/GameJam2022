using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Audio;
using UnityEngine.Animations;

/*

*/

public class CharacterAnimationGraphCharacterController : MonoBehaviour {
  [SerializeField] CharacterAnimationGraph AnimationGraph;
  [SerializeField] AudioClip AudioClip;
  [SerializeField] AnimationClip AnimationClip;
  [SerializeField] TimelineAsset TimelineAsset;

  void Update() {
    if (Input.GetKeyDown(KeyCode.Q)) {
      var playable = AudioClipPlayable.Create(AnimationGraph.Graph, AudioClip, false);
      AnimationGraph.DefaultSlot.PlayAudio(playable);
    }
    if (Input.GetKeyDown(KeyCode.W)) {
      var playable = AnimationClipPlayable.Create(AnimationGraph.Graph, AnimationClip);
      AnimationGraph.DefaultSlot.PlayAnimation(playable);
    }
    if (Input.GetKeyDown(KeyCode.E)) {
      var timelinePlayable = TimelineAsset.CreatePlayable(AnimationGraph.Graph, gameObject);
      var outputTrackCount = TimelineAsset.outputTrackCount;
      // N.B. You MUST do this to open up all the ports associated with inputs/tracks
      UnityEngine.Playables.PlayableExtensions.SetOutputCount(timelinePlayable, 5);
      for (var outputTrackIndex = 0; outputTrackIndex < outputTrackCount; outputTrackIndex++) {
        var track = TimelineAsset.GetOutputTrack(outputTrackIndex);
        foreach (var output in track.outputs) {
          var targetType = output.outputTargetType;
          if (targetType == typeof(Animator)) {
            AnimationGraph.DefaultSlot.PlayAnimation(timelinePlayable, outputTrackIndex);
          } else if (targetType == typeof(AudioSource)) {
            AnimationGraph.DefaultSlot.PlayAudio(timelinePlayable, outputTrackIndex);
          }
        }
      }
    }
  }
}