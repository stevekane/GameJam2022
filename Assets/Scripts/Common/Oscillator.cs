using UnityEngine;

public class Oscillator : MonoBehaviour {
  public float Amplitude = 1.0f;
  public float Period = 1.0f;
  public float Offset = 0;
  public Vector3 Direction = Vector3.up;

  Vector3 StartPosition;

  void Start() {
    StartPosition = transform.localPosition;
  }

  void LateUpdate() {
    var pos = StartPosition + Direction * Amplitude * Mathf.Sin(2.0f * Mathf.PI * Time.time / Period + Offset);
    transform.localPosition = pos;
  }
}