using UnityEngine;

namespace ActionsAndAI {
  public interface IAction {
    public string Name { get; }
    public ButtonCode ButtonCode { get; }
    public ButtonPressType ButtonPressType { get; }
    public bool CanStart();
    public void OnStart();
  }

  public abstract class AbstractAction : MonoBehaviour, IAction {
    public abstract string Name { get; }
    public abstract ButtonCode ButtonCode { get; }
    public abstract ButtonPressType ButtonPressType { get; }
    public abstract bool CanStart();
    public abstract void OnStart();

    void OnEnable() {
      gameObject.GetComponentInParent<ActionManager>()?.Actions.Add(this);
    }

    void OnDisable() {
      gameObject.GetComponentInParent<ActionManager>()?.Actions.Add(this);
    }
  }
}