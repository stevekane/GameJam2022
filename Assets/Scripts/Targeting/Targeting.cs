using System;
using System.Collections.Generic;
using UnityEngine;

public static class Targeting {
  public static X FindTarget<X>(
  IEnumerable<X> xs,
  Func<X, bool> predicate,
  Func<X, X, X> choose,
  X target = default(X)) {
    foreach (var x in xs) {
      if (predicate(x)) {
        target = choose(target, x);
      }
    }
    return target;
  }

  public static Collider StandardTarget(
  Transform t,
  float radius,
  LayerMask layerMask,
  QueryTriggerInteraction triggerInteraction,
  Collider[] colliders) {
    bool IsVisible(Collider c) => c.transform.IsVisibleFrom(t.position, layerMask, triggerInteraction);
    float Score(Collider c) {
      if (c) {
        var delta = c.transform.position-t.position;
        var toDest = delta.normalized;
        var angleScore = Vector3.Dot(t.forward, toDest);
        var distanceScore = 1-delta.magnitude/radius;
        return angleScore+distanceScore;
      } else {
        return 0;
      }
    }
    Collider BestScore(Collider a, Collider b) => Score(a) > Score(b) ? a : b;

    var hitCount = Physics.OverlapSphereNonAlloc(t.position, radius, colliders, layerMask, triggerInteraction);
    var hits = colliders[..hitCount];
    return FindTarget(hits, IsVisible, BestScore, null);
  }
}