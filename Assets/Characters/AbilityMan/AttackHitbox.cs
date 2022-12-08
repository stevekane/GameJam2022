using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour {
  public Collider Collider;
  public Action<Hurtbox> TriggerEnter;
  public Action<Hurtbox> TriggerStay;
  public Action<Hurtbox> TriggerExit;
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out Hurtbox hurtbox)) {
      TriggerEnter?.Invoke(hurtbox);
    }
  }
  void OnTriggerStay(Collider c) {
    if (c.TryGetComponent(out Hurtbox hurtbox)) {
      TriggerStay?.Invoke(hurtbox);
    }
  }
  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out Hurtbox hurtbox)) {
      TriggerExit?.Invoke(hurtbox);
    }
  }
}