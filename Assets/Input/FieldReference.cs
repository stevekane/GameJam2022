using System;
using UnityEngine;

public class FieldReference {}

[Serializable]
public class FieldReference<T,F> : FieldReference where T : MonoBehaviour {
  public T Target;
  public string FieldName;
  public F Value {
    get {
      return (F)Target.GetType()?.GetField(FieldName)?.GetValue(Target);
    }
  }
}