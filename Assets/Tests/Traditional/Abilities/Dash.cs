using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Traditional {
  public class Dash : MonoBehaviour {
    [SerializeField] MoveDelta MoveDelta;
    [SerializeField] MoveSpeed MoveSpeed;
    [SerializeField] TurnSpeed TurnSpeed;
    [SerializeField] AnimationGraph AnimationGraph;
    [SerializeField] AnimationSpecification AnimationSpecification;
    [SerializeField] AnimationMontageSpecification AnimationMontageSpecification;
    [SerializeField] float DashSpeed = 25;

    bool IsDone => Animation.IsNull() || !Animation.IsValid() || Animation.IsDone();
    AnimationClipPlayable Animation;
    AnimationMontagePlayable Montage;

    void OnEnable() {
      Animation = AnimationGraph.Play(AnimationSpecification);
      enabled = !IsDone;
    }

    void OnDisable() {
      if (!IsDone) {
        AnimationGraph.Stop(Animation);
      }
    }

    void FixedUpdate() {
      MoveDelta.Add(transform.forward * DashSpeed * Time.fixedDeltaTime);
      MoveSpeed.Mul(0);
      TurnSpeed.Mul(.25f);
      enabled = !IsDone;
    }

    public void StartTestAnim() {
      Debug.Log("Test animation started!");
    }

    public void EndTestAnim() {
      Debug.Log("Test animation ended!");
    }

    public void TestAnimMoment() {
      Debug.Log("You had a test animation moment!");
    }
  }
}