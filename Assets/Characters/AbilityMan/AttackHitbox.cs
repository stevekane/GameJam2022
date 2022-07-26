using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour {
  public Collider Collider;
  public Action<Collider> TriggerEnter;
  public Action<Collider> TriggerStay;
  public Action<Collider> TriggerExit;
  void OnTriggerEnter(Collider c) => TriggerEnter?.Invoke(c);
  void OnTriggerStay(Collider c) => TriggerStay?.Invoke(c);
  void OnTriggerExit(Collider c) => TriggerExit?.Invoke(c);
}