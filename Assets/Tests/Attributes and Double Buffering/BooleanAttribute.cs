using System;
using UnityEngine;

[Serializable]
public struct BooleanValue {
  [SerializeField]
  int PersistentFalseCount;
  [SerializeField]
  int FalseCount;
  public bool Value => PersistentFalseCount + FalseCount <= 0;
  public void Vote(bool b) => FalseCount += (b ? 0 : 1);
  public void Set(bool b) => PersistentFalseCount += (b ? -1 : 1);
  public void Reset() => FalseCount = 0;
}

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class BooleanAttribute : MonoBehaviour {
  [field:SerializeField]
  public BooleanValue Current { get; protected set; }
  public BooleanValue Next;
  void LateUpdate() {
    Current = Next;
    Next.Reset();
  }
}