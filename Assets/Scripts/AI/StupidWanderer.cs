using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class StupidWanderer : MonoBehaviour {
  [Header("Components")]
  public NavMeshAgent Agent;

  [Header("Config")]
  public LayerMask FriendLayerMask;
  public LayerMask PreyLayerMask;
  public float GroupWeight;
  public float DistanceWeight;
  public float MaxVisibilityDistance = 1000;
  public float DesiredFriendDistance = 5;
  public float DesiredPreyDistance = 10;
  public float FriendSearchRadius = 15;

  [Header("State")]
  public bool PreyIsVisible;
  public bool VisibleToPrey;
  public bool InFrontOfPrey;
  public bool InPreyLineOfSight;
  public Transform Prey;
  public Vector3? LastKnownPreyPosition;
  public Vector3 GroupGradient;
  public float GroupConstraint;
  public Vector3 DistanceGradient;
  public float DistanceConstraint;

  List<T> NearbyFriends<T>(T ignore, Vector3 p, float radius, LayerMask mask) where T : MonoBehaviour {
    var friends = new List<T>();
    var colliders = Physics.OverlapSphere(p, radius, mask);
    foreach (var collider in colliders) {
      if (collider.TryGetComponent(out T t) && t != ignore) {
        friends.Add(t);
      }
    }
    return friends;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime; 
    var eyePosition = transform.position+Vector3.up;

    PreyIsVisible = Prey.IsVisibleFrom(eyePosition, PreyLayerMask);
    VisibleToPrey = PreyIsVisible;
    InFrontOfPrey = transform.position.IsInFrontOf(Prey);
    InPreyLineOfSight = VisibleToPrey && InFrontOfPrey;

    {
      var nearbyFriends = NearbyFriends<StupidWanderer>(null, eyePosition, FriendSearchRadius, FriendLayerMask);
      if (nearbyFriends.Count > 0) {
        var centerOfMass = nearbyFriends.Select(f => f.transform.position).Sum(v => v)/(float)nearbyFriends.Count;
        var delta = transform.position-centerOfMass;
        var direction = delta.normalized;
        GroupGradient = direction;
        GroupConstraint = DesiredFriendDistance-delta.magnitude;
        GroupConstraint = Mathf.Max(0, GroupConstraint); // Only care if too close
      } else {
        GroupGradient = Vector3.zero;
        GroupConstraint = 0;
      }
    }

    {
      var delta = transform.position-Prey.transform.position;
      var direction = delta.normalized;
      var LOSmultiplier = InPreyLineOfSight ? 3 : 1;
      DistanceGradient = direction;
      DistanceConstraint = LOSmultiplier*DesiredPreyDistance-delta.magnitude;
    }

    {
      LastKnownPreyPosition = PreyIsVisible ? Prey.position : LastKnownPreyPosition;
    }

    {
      if (PreyIsVisible) {
        var newPosition = 
          transform.position +
          DistanceWeight*dt*Agent.speed*DistanceConstraint*DistanceGradient +
          GroupWeight*dt*Agent.speed*GroupConstraint*GroupGradient;
        Agent.SetDestination(newPosition);
      } else if (LastKnownPreyPosition.HasValue) {
        var newPosition = 
          transform.position +
          dt*Agent.speed*(LastKnownPreyPosition.Value-transform.position) +
          GroupWeight*dt*Agent.speed*GroupConstraint*GroupGradient;
        Agent.SetDestination(newPosition);
      } else {
        Agent.SetDestination(transform.position);
      }
    }
  }
}