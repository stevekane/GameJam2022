using UnityEngine;

public abstract class SampleAbility : MonoBehaviour {
  public abstract bool IsRunning { get; protected set; }
  public abstract void Stop();
}