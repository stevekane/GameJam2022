using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackTarget {
  public GameObject GameObject;
  public float Distance;
  public float Angle;
  public int Priority;
}

public class MeleeAttackTargeting : MonoBehaviour {
  public LayerMask LayerMask;
  public QueryTriggerInteraction TriggerInteraction;
  public float MaxDistance;
  public float MaxAngle;
  public GameObject BestCandidate;
  public HashSet<GameObject> Candidates;
  public HashSet<GameObject> Victims;

  void Awake() {
    Candidates = new();
    Victims = new();
  }

  void FixedUpdate() {
    var allTargets = FindObjectsOfType<TargetDummyController>();
    var bestCandidateDistance = float.MaxValue;
    BestCandidate = null;
    Candidates.Clear();
    foreach (var target in allTargets) {
      var p0 = transform.position;
      var p1 = target.transform.position;
      var delta = p1-p0;
      var distance = delta.magnitude;
      var toTarget = delta.normalized;
      var inRange = distance <= MaxDistance;
      var angle = Vector3.Angle(transform.forward, toTarget);
      var inView = angle <= MaxAngle;
      var eyeOffset = Vector3.up;
      var ray = new Ray(transform.position + eyeOffset, p1-p0);
      if (inRange && inView && Physics.Raycast(ray, out var hit, distance, LayerMask, TriggerInteraction)) {
        if (hit.transform.TryGetComponent(out TestHurtBox hurtbox) && hurtbox.Owner == target.gameObject) {
          if (!BestCandidate || distance < bestCandidateDistance) {
            BestCandidate = target.gameObject;
            bestCandidateDistance = distance;
          }
          Candidates.Add(target.gameObject);
        }
      }
    }
    Victims.RemoveWhere(victim => !Candidates.Contains(victim));
  }

  #if UNITY_EDITOR
  void OnDrawGizmos() {
    if (Candidates == null)
      return;

    var eyeOffset = Vector3.up;
    foreach (var candidate in Candidates) {
      Gizmos.DrawLine(transform.position + eyeOffset, candidate.transform.position + eyeOffset);
    }
  }
  #endif
}