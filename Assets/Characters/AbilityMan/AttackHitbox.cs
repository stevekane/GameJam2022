using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour {
  public Collider Collider;
  public Action<Transform> TriggerEnter;
  public Action<Transform> TriggerStay;
  public Action<Transform> TriggerExit;
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out Hurtbox hurtbox)) {
      TriggerEnter?.Invoke(hurtbox.Defender.transform);
    }
  }
  void OnTriggerStay(Collider c) {
    if (c.TryGetComponent(out Hurtbox hurtbox)) {
      TriggerStay?.Invoke(hurtbox.Defender.transform);
    }
  }
  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out Hurtbox hurtbox)) {
      TriggerExit?.Invoke(hurtbox.Defender.transform);
    }
  }
}