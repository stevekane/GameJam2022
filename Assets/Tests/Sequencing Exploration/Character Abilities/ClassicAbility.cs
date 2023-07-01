using System;
using System.Threading.Tasks;

public abstract class ClassicAbility : SimpleAbility {
  int RunningTaskCount;
  TaskScope Scope = new();
  public override bool IsRunning => RunningTaskCount > 0;
  public override void Stop() {
    Tags = default;
    AddedToOwner = default;
    Scope.Dispose();
    Scope = new();
  }
  public AbilityAction Main;

  void Awake() {
    Main.CanRun = true;
    Main.Ability = this;
    Main.Listen(FireMain);
  }

  void OnDestroy() {
    Scope?.Dispose();
  }

  void FireMain() {
    Scope.Run(Runner(MainAction));
  }

  protected TaskFunc Runner(Func<TaskScope, Task> f) => async scope => {
    try {
      RunningTaskCount++;
      await f(scope);
    } finally {
      RunningTaskCount--;
      if (RunningTaskCount == 0) {
        Stop();
      }
    }
  };

  public abstract Task MainAction(TaskScope scope);
}