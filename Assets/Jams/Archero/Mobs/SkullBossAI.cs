using KinematicCharacterController;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class SkullBossAI : AI {
    [Serializable]
    public class Generation {
      public float ModelScale;
      public float MoveSpeedBase;
      public int ChildCount;
      // Could also scale damage/health?
    }
    public GameObject Model;
    public DeathSplit SplitPrefab;
    public Generation[] Generations;
    Generation CurrentGeneration;

    Attributes Attributes => GetComponent<Attributes>();

    public override void Start() {
      var split = GetComponent<DeathSplit>();
      CurrentGeneration = Generations[split.Generation];

      split.SplitInto = Enumerable.Repeat(SplitPrefab, CurrentGeneration.ChildCount).ToArray();
      var s = CurrentGeneration.ModelScale;
      Model.transform.localScale = new Vector3(s, s, s);
      Velocity = Quaternion.Euler(0, split.SplitIndex * 90, 0) * new Vector3(1, 0, 1).normalized;
      Velocity = Attributes.GetValue(AttributeTag.MoveSpeed, CurrentGeneration.MoveSpeedBase) * Velocity;

      base.Start();
    }

    public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
      Debug.Log($"Hit {hitCollider.gameObject}, rotating 90 from {Velocity}; normal={hitNormal} dot={Vector3.Dot(hitNormal, Velocity)}");
      ScriptedVelocity = hitNormal;  // Push them away from the wall.
      Velocity = Vector3.Reflect(Velocity.normalized, hitNormal);
      Velocity = Attributes.GetValue(AttributeTag.MoveSpeed, CurrentGeneration.MoveSpeedBase) * Velocity;
    }

    // Only behavior is reacting to collisions above.
    public override async Task Behavior(TaskScope scope) {
      await scope.Tick();
    }
  }
}