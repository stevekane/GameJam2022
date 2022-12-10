using System;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;

public delegate IEnumerator AbilityMethod();
public delegate Task AbilityMethodTask(TaskScope scope);

[Serializable]
public class AbilityMethodReference {
  public Ability Ability;
  public string MethodName;

  public AbilityMethod GetMethod() {
    var methodInfo = Ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), Ability, methodInfo);
  }
  public AbilityMethodTask GetMethodTask() {
    var methodInfo = Ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethodTask)Delegate.CreateDelegate(typeof(AbilityMethodTask), Ability, methodInfo);
  }
}

// Same as above but used if the Ability is the owning class.
[Serializable]
public class AbilityMethodReferenceSelf {
  public string MethodName;

  public AbilityMethod GetMethod(Ability ability) {
    var methodInfo = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), ability, methodInfo);
  }
  public AbilityMethodTask GetMethodTask(Ability ability) {
    var methodInfo = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethodTask)Delegate.CreateDelegate(typeof(AbilityMethodTask), ability, methodInfo);
  }
  public bool IsTask(Ability ability) {
    var methodInfo = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (methodInfo.ReturnType == typeof(Task));
  }
}