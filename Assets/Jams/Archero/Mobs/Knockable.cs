using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public class Knockable : MonoBehaviour {
    AI AI;
    Status Status;
    SimpleAbilityManager AbilityManager;

    public void Start() {
      this.InitComponent(out AI);
      this.InitComponent(out Status);
      this.InitComponent(out AbilityManager);
    }

    void OnHurt(HitParams hitParams) {
      Debug.Log($"Knockback: {AbilityManager.Tags.HasAnyFlags(AbilityTag.Uninterruptible)}: {hitParams.GetKnockbackStrength() * hitParams.KnockbackVector}");
      if (!AbilityManager.Tags.HasAnyFlags(AbilityTag.Uninterruptible))
        Status.Add(new KnockbackEffect(AI, hitParams.GetKnockbackStrength() * hitParams.KnockbackVector));
    }
  }
}