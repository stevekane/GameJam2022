using UnityEngine;

namespace ActionsAndAI {
  public abstract class AbstractActionBehavior : MonoBehaviour {
    public abstract bool CanStart();
    public abstract void OnStart();

    void OnEnable() {
      gameObject.GetComponentInParent<ActionManager>()?.Actions.Add(this);
    }

    void OnDisable() {
      gameObject.GetComponentInParent<ActionManager>()?.Actions.Add(this);
    }
  }

  public abstract class AbstractAxisActionBehavior : MonoBehaviour {
    public abstract bool CanStart();
    public abstract void OnStart(AxisState axisState);

    void OnEnable() {
      gameObject.GetComponentInParent<ActionManager>()?.AxisActions.Add(this);
    }

    void OnDisable() {
      gameObject.GetComponentInParent<ActionManager>()?.AxisActions.Add(this);
    }
  }
}