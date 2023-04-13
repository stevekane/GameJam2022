using System.Threading.Tasks;
using System;
using UnityEngine;

[Serializable]
public class PotentialAction {
  public static bool True() => true;
  public static bool False() => false;

  public EventSource Source;
  public Func<Boolean> IsSatisfied;
  public Action Action;
  public bool TryFire() {
    if (IsSatisfied()) {
      Fire();
      return true;
    } else {
      return false;
    }
  }
  public void Fire() {
    Action?.Invoke();
    Source?.Fire();
  }
  public PotentialAction(Func<Boolean> isSatisfied, Action action) {
    Source = new();
    IsSatisfied = isSatisfied;
    Action = action;
  }
  public async Task ListenFor(TaskScope scope) {
    try {
      IsSatisfied = True;
      await scope.ListenFor(Source);
    } finally {
      IsSatisfied = False;
    }
  }
}

public class MockInteractAbility : SampleAbility {
  public PotentialAction InteractAction;
  public PotentialAction ConfirmAction;
  public PotentialAction CancelAction;
  public override bool IsRunning { get; protected set; }
  public override void Stop() {
    IsRunning = false;
    Debug.Log("Interaction canceled");
  }

  void Awake() {
    InteractAction = new(CanInteract, Interact);
    ConfirmAction = new(CanConfirm, Confirm);
    CancelAction = new(CanCancel, Cancel);
  }

  bool CanInteract() => !IsRunning;
  bool CanConfirm() => IsRunning;
  bool CanCancel() => IsRunning;

  public void Interact() {
    Debug.Log("Interact");
    IsRunning = true;
  }
  void Confirm() {
    Debug.Log("Confirm");
    IsRunning = false;
  }
  void Cancel() {
    Debug.Log("Cancel");
    IsRunning = false;
  }
}