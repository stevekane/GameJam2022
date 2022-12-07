using System;
using UnityEngine;

public class Elevator : MonoBehaviour {
  [SerializeField] Rigidbody Car;
  [SerializeField] float MoveSpeed = 5f;
  [SerializeField] float SnapDistance = .1f;

  public EventSource<Transform> SetTarget = new();

  TaskScope Scope = new();

  async void Start() {
    try {
      await Scope.Repeat(async delegate {
        var target = await Scope.ListenFor(SetTarget);
        while (Vector3.Distance(Car.position, target.position) > SnapDistance) {
          await Scope.Tick();
          var moveDistance = MoveSpeed*Time.fixedDeltaTime;
          var next = Vector3.MoveTowards(Car.position, target.position, moveDistance);
          Car.MovePosition(next);
        }
        Car.MovePosition(target.position);
      });
    } catch (Exception c) {
      Debug.LogWarning(c.Message);
    }
  }
  void OnDestroy() {
    Scope.Cancel();
  }
}