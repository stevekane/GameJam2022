using UnityEngine;

public abstract class SampleAbility : MonoBehaviour {
  public abstract bool IsRunning { get; protected set; }
  public abstract void Stop();
  public bool TryStop() {
    if (IsRunning) {
      Stop();
      return true;
    } else {
      return false;
    }
  }
}