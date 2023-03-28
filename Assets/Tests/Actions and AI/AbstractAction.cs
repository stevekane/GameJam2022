using UnityEngine;

namespace ActionsAndAI {
  public interface IAction {
    public string Name { get; }
    public ButtonCode ButtonCode { get; }
    public ButtonPressType ButtonPressType { get; }
    public bool CanStart();
    public void OnStart();
  }

  public interface IAxisAction {
    public string Name { get; }
    public AxisCode AxisCode { get; }
    public bool CanStart();
    public void OnStart(AxisState axisState);
  }

  public abstract class AbstractAction : IAction {
    public abstract string Name { get; set; }
    public abstract ButtonCode ButtonCode { get; set; }
    public abstract ButtonPressType ButtonPressType { get; set; }
    public abstract bool CanStart();
    public abstract void OnStart();
  }

  public abstract class AbstractAxisAction : IAxisAction {
    public abstract string Name { get; set; }
    public abstract AxisCode AxisCode { get; set; }
    public abstract bool CanStart();
    public abstract void OnStart(AxisState axisState);
  }

  public abstract class AbstractActionBehavior : MonoBehaviour, IAction {
    public abstract string Name { get; set; }
    public abstract ButtonCode ButtonCode { get; set; }
    public abstract ButtonPressType ButtonPressType { get; set; }
    public abstract bool CanStart();
    public abstract void OnStart();

    void OnEnable() {
      gameObject.GetComponentInParent<ActionManager>()?.Actions.Add(this);
    }

    void OnDisable() {
      gameObject.GetComponentInParent<ActionManager>()?.Actions.Add(this);
    }
  }

  public abstract class AbstractAxisActionBehavior : MonoBehaviour, IAxisAction {
    public abstract string Name { get; set; }
    public abstract AxisCode AxisCode { get; set; }
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