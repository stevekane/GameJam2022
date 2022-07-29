using System.Collections;
using UnityEngine;

abstract public class Ability : MonoBehaviour {
  public bool IsComplete { get; private set; } = true;
  public IEnumerator Wrapper() {
    IsComplete = false;
    BeforeBegin();
    AfterBegin();
    yield return Routine();
    BeforeEnd();
    StopAllCoroutines();
    AfterEnd();
    IsComplete = true;
  }
  public void Begin() {
    StopAllCoroutines();
    StartCoroutine(Wrapper());
  }
  public void End() {
    BeforeEnd();
    StopAllCoroutines();
    AfterEnd();
    IsComplete = true;
  }
  public abstract IEnumerator Routine();
  public virtual void BeforeBegin() {}
  public virtual void AfterBegin() {}
  public virtual void BeforeEnd() {}
  public virtual void AfterEnd() {}
}