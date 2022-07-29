using System.Collections;

public abstract class AbilityTask : IEnumerator {
  public IEnumerator Enumerator;
  public object Current { get => Enumerator.Current; }
  public bool MoveNext() => Enumerator.MoveNext();
  public void Dispose() => Enumerator = null;
  public void Reset() => Enumerator = Routine();
  public abstract IEnumerator Routine();
}