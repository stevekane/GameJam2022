using System.Collections;
using UnityEngine;

public class DashManagerAbility : Ability {
  [SerializeField] GameObject Owner;
  [SerializeField] Timeval DashThreshold = Timeval.FromMillis(60, 60);

  bool IsKeyDown;
  int FramesDown;

  void OnEnable() {
    Activate();
  }

  void OnDisable() {
    Stop();
  }

  public override void Activate() {
    InputManager.Instance.L1.JustDown.Action += OnKeyDown;
    InputManager.Instance.L1.JustUp.Action += OnKeyUp;
    base.Activate();
  }

  public override void Stop() {
    InputManager.Instance.L1.JustDown.Action -= OnKeyDown;
    InputManager.Instance.L1.JustUp.Action -= OnKeyUp;
    base.Stop();
  }

  void OnKeyDown() {
    IsKeyDown = true;
  }

  void OnKeyUp() {
    IsKeyDown = false;
  }

  protected override IEnumerator MakeRoutine() {
    while (true) {
      FramesDown = IsKeyDown ? FramesDown+1 : 0;
      yield return null;
    }
  }
}