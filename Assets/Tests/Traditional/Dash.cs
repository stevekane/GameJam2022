using UnityEngine;

namespace Traditional {
  public class Dash : MonoBehaviour {
    [SerializeField] MoveDelta MoveDelta;
    [SerializeField] MoveSpeed MoveSpeed;
    [SerializeField] TurnSpeed TurnSpeed;
    [SerializeField] InputManager InputManager;
    [SerializeField] AnimationGraph AnimationGraph;
    [SerializeField] AnimationSpecification AnimationSpecification;
    [SerializeField] Timeval Cooldown = Timeval.FromSeconds(2);
    [SerializeField] float DashSpeed = 25;
    bool IsAvailable => CooldownRemaining > 0;
    bool IsActive;
    float CooldownRemaining;

    void Start() {
      InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Listen(Activate);
    }

    void OnDestroy() {
      InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Unlisten(Activate);
    }

    void FixedUpdate() {
      CooldownRemaining = Mathf.Max(0, CooldownRemaining-1);
      if (IsActive) {
        MoveDelta.Add(transform.forward * DashSpeed * Time.fixedDeltaTime);
        MoveSpeed.Mul(0);
        Debug.Log($"MOVESPEED {MoveSpeed.Value} MOVEDELTA {MoveDelta.Value}");
      }
    }

    void Activate() {
      if (!IsAvailable) {
        CooldownRemaining = Cooldown.Ticks;
        AnimationGraph.Play(AnimationSpecification);
      }
    }

    void Stop() {
      // TODO: Probably stop the Animation as well?
      IsActive = false;
    }

    public void StartTestAnim() {
      IsActive = true;
      Debug.Log("Test animation started!");
    }

    public void EndTestAnim() {
      Debug.Log("Test animation ended!");
    }

    public void TestAnimMoment() {
      IsActive = false;
      Debug.Log("You had a test animation moment!");
    }
  }
}