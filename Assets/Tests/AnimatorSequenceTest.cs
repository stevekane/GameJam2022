using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.UI;

public class AnimatorSequenceTest : MonoBehaviour {
  public PlayableGraph Graph;
  public SequencePlayable SequencePlayable;
  public AnimationClip Clip1;
  public AnimationClip Clip2;
  public Animator Animator;
  public Slider SeekSlider;

  public void Start() {
    Graph = PlayableGraph.Create();
    var sequence = ScriptPlayable<SequencePlayable>.Create(Graph, 1);
    var output = AnimationPlayableOutput.Create(Graph, "ToAnimator", Animator);
    SequencePlayable = sequence.GetBehaviour();
    SequencePlayable.AddClip(Clip1);
    SequencePlayable.AddClip(Clip2);
    output.SetSourcePlayable(sequence, 0);
    Graph.Play();
    SeekSlider.minValue = 0;
    SeekSlider.maxValue = Clip1.length+Clip2.length;
    SeekSlider.onValueChanged.AddListener(SequencePlayable.SetSeeker);
  }

  public void OnDestroy() {
    Graph.Destroy();
    SeekSlider.onValueChanged.RemoveListener(SequencePlayable.SetSeeker);
  }
}