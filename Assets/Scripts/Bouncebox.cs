using System;
using UnityEngine;

public class Bouncebox : MonoBehaviour {
  public Collider Collider;
  public Action<Collider> OnHit;

  private void Awake() {
    Collider = GetComponent<Collider>();
  }
  private void OnTriggerEnter(Collider other) {
    OnHit?.Invoke(other);
  }
}