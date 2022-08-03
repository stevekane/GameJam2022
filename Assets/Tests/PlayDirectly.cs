using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Animator))]
public class PlayDirectly : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] AnimationClip Clip;

  PlayableGraph Graph;
  AnimationClipPlayable ClipPlayable;

  void Start() {
    Graph = PlayableGraph.Create();
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    ClipPlayable = AnimationClipPlayable.Create(Graph, Clip);
    ClipPlayable.SetDuration(Clip.length);
    AnimationPlayableOutput.Create(Graph, "Animation", Animator).SetSourcePlayable(ClipPlayable);
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void Update() {
    if (!Graph.IsDone())
      Graph.Evaluate(0);
  }

  void FixedUpdate() {
    if (!Graph.IsDone())
      Graph.Evaluate(Time.fixedDeltaTime);
  }

  void OnLightAttack() {
    Debug.Log("Light attack!");
  }
}