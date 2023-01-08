using System.Collections.Generic;
using UnityEngine;

public class GrapplePoint : MonoBehaviour {
  [SerializeField] float TurnSpeed = 90;

  public List<Vector3> Sources = new();

  Vector3 BaseForward;

  void Awake() {
    BaseForward = transform.forward;
  }

  void OnEnable() {
    GrapplePointManager.Instance.Points.Add(this);
  }
  void OnDisable() {
    GrapplePointManager.Instance.Points.Remove(this);
  }

  void FixedUpdate() {
    if (Sources.Count > 0) {
      var forward = Vector3.zero;
      foreach (var p in Sources) {
        forward += (p - transform.position).normalized;
      }
      forward = forward.XZ();
      var desired = Quaternion.LookRotation(forward);
      var maxRotation = Time.fixedDeltaTime * TurnSpeed;
      transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, maxRotation);
    } else {
      var desired = Quaternion.LookRotation(BaseForward);
      var maxRotation = Time.fixedDeltaTime * TurnSpeed;
      transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, maxRotation);
    }
    Sources.Clear();
  }
}