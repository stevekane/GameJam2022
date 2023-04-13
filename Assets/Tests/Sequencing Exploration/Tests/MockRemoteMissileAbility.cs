using System.Threading.Tasks;
using UnityEngine;

public class MockRemoteMissileAbility : SampleAbility {
  public PotentialAction FireAction;
  public PotentialAction DetonateAction;
  public override bool IsRunning { get; protected set; }
  public override void Stop() {
    Scope.Dispose();
    Debug.Log("Remote Missile Canceled");
  }

  TaskScope Scope;

  void Awake() {
    FireAction = new(CanFire, Fire);
    DetonateAction = new(PotentialAction.False, null);
  }

  bool CanFire() {
    return !IsRunning;
  }

  void Fire() {
    Scope = new();
    Scope.Run(Routine);
  }

  async Task Routine(TaskScope scope) {
    try {
      IsRunning = true;
      Debug.Log("Fired");
      await DetonateAction.ListenFor(scope);
      Debug.Log("Detonated");
    } finally {
      IsRunning = false;
    }
  }
}