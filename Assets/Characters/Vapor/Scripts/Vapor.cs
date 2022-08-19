using System;
using UnityEngine;

public class Vapor : MonoBehaviour, IWireRider {
  [SerializeField] Timeval WireRide;

  public int PunchCycleIndex;

  public void RideWire(Wire wire) {
    //Wire = wire;
  }

  void Awake() {
  }

  void FixedUpdate() {
    //Animator.SetBool("WireRiding", Motion == Motion.WireRiding);
  }
}