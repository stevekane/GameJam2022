using System;
using System.Collections.Generic;
using UnityEngine;

// Common routine for handling hitbox collision + processing the hit.
public static class HitHandler {
  public static TaskFunc Loop(TriggerEvent hitbox, HitParams hitParams, Action<Hurtbox> onHit = null) => Loop(hitbox, null, hitParams, onHit);
  public static TaskFunc Loop(TriggerEvent hitbox, Parrybox parrybox, HitParams hitParams, Action<Hurtbox> onHit = null) => async (TaskScope scope) => {
    //GameObject dbg = null;
    try {
      Parrybox parried = null;
      List<Hurtbox> hits = new();
      int lastHit = 0;
      using var listener = new ScopedListener<Collider>(hitbox.OnTriggerStaySource, hit => {
        if (hit.TryGetComponent(out Parrybox pb) && pb.TryParry(hitParams))
          parried = pb;
        if (hit.TryGetComponent(out Hurtbox hurtbox) && !hits.Contains(hurtbox) && hurtbox.CanBeHurtBy(hitParams))
          hits.Add(hurtbox);
      });
      hitbox.EnableCollision = true;
      if (parrybox) parrybox.EnableCollision = true;
      //if (parrybox) dbg = VFXManager.Instance.TrySpawnEffect(VFXManager.Instance.DebugIndicatorPrefab, parrybox.transform.position, parrybox.transform.rotation);
      while (!parried) {
        while (lastHit < hits.Count) {
          var hb = hits[lastHit++];
          if (hb.CanBeHurtBy(hitParams)) {
            onHit?.Invoke(hb);
            hb.TryAttack(hitParams.Clone());
          }
        }
        await scope.Tick();
      }
    } catch (OperationCanceledException) {
    } catch (Exception ex) {
      Debug.LogException(ex);
    } finally {
      //GameObject.Destroy(dbg);
      hitbox.EnableCollision = false;
      if (parrybox) parrybox.EnableCollision = false;
    }
  };

  public static TaskFunc LoopTimeline(TriggerEvent hitbox, Parrybox parrybox, HitParams hitParams, Action<Hurtbox> onHit = null) => async (TaskScope scope) => {
    Parrybox parried = null;
    List<Hurtbox> hits = new();
    int lastHit = 0;
    using var listener = new ScopedListener<Collider>(hitbox.OnTriggerStaySource, hit => {
      Debug.Log($"Hit: {hit}");
      if (hit.TryGetComponent(out Parrybox pb) && pb.TryParry(hitParams))
        parried = pb;
      if (hit.TryGetComponent(out Hurtbox hurtbox) && !hits.Contains(hurtbox) && hurtbox.CanBeHurtBy(hitParams))
        hits.Add(hurtbox);
    });
    while (!parried) {
      while (lastHit < hits.Count) {
        var hb = hits[lastHit++];
        if (hb.CanBeHurtBy(hitParams)) {
          onHit?.Invoke(hb);
          hb.TryAttack(hitParams.Clone());
        }
      }
      await scope.Tick();
    }
  };
}