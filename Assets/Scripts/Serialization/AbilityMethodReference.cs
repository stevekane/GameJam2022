using System;
using System.Reflection;

[Serializable]
public class AbilityMethodReference {
  public Ability Ability;
  public string MethodName;

  public AbilityMethod GetMethod() {
    if (!Ability || MethodName.Length == 0) return null;
    var methodInfo = Ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), Ability, methodInfo);
  }
}
