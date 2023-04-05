using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ActionEvent : MonoBehaviour {
  public UnityEvent Event;
  public bool IsAvailable;
  public async Task ListenFor(TaskScope scope) {
    bool fired = false;
    void OnFire() => fired = true;
    bool Fired() => fired;
    try {
      IsAvailable = true;
      Event.AddListener(OnFire);
      await scope.Until(Fired);
    } finally {
      Event.RemoveListener(OnFire);
      IsAvailable = false;
    }
  }
}

public class NestedComplexAbility : MonoBehaviour {
  [SerializeField] ActionEvent Select;
  [SerializeField] ActionEvent Rotate;
  [SerializeField] ActionEvent Commit;
  [SerializeField] int Selected;

  TaskScope Scope;

  public void Start() {
    Scope?.Dispose();
    Scope = new();
    Scope.Run(Run);
  }

  public void Stop() {
    Scope.Dispose();
    Scope = null;
  }

  async Task Run(TaskScope scope) {
    try {
      await Select.ListenFor(scope);
      Selected = 1;
      await Build(scope);
    } finally {
    }
  }

  async Task Build(TaskScope scope) {
  }
}