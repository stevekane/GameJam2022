using System;
using System.Collections.Generic;
using UnityEngine;

// Common routine for handling hitbox collision + processing the hit.
public static class HitHandler {
  public static TaskFunc Loop(TriggerEvent hitbox, HitParams hitParams, Action<Hurtbox> onHit = null) => async (TaskScope scope) => {
    try {
      List<Hurtbox> hits = new();
      int lastHit = 0;
      using var listener = new ScopedListener<Collider>(hitbox.OnTriggerStaySource, hit => {
        if (hit.TryGetComponent(out Hurtbox hurtbox) && !hits.Contains(hurtbox))
          hits.Add(hurtbox);
      });
      hitbox.enableCollision = true;
      while (true) {
        while (lastHit < hits.Count) {
          onHit?.Invoke(hits[lastHit]);
          hits[lastHit++].TryAttack(hitParams.Clone());
        }
        await scope.Tick();
      }
    } finally {
      hitbox.enableCollision = false;
    }
  };
}