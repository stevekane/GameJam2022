using System;
using UnityEngine;

[Serializable]
public class FieldReference<T,F> where T : MonoBehaviour {
  public T Target;
  public string FieldName;
  public F Value {
    get {
      return (F)Target.GetType()?.GetField(FieldName)?.GetValue(Target);
    }
  }
}