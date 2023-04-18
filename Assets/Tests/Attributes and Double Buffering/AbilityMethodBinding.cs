using System;

[Serializable]
public class AbilityMethodBinding {
  public AbilityMethodReference Method;
  public SimpleAbilityManager AbilityManager;
  public SampleAction Action;
  public void Update() {
    Action.Satisfied = AbilityManager.CanInvoke(Method.GetMethod());
  }
  public void Bind() {
    Action.EventSource.Listen(Fire);
  }
  public void Unbind() {
    Action.EventSource.Unlisten(Fire);
  }
  void Fire() {
    AbilityManager.TryInvoke(Method.GetMethod());
  }
}