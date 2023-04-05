using UnityEngine;

public class CrafterHurtbox : Hurtbox {
  [SerializeField] Crafter Machine;

  public override bool CanBeHurtBy(HitParams hitParams) {
    return (hitParams.Attacker.GetComponent<Player>() != null);
  }
  public override bool TryAttack(HitParams hitParams) {
    ItemFlowManager.Instance.AddCraftRequest(Machine);
    return true;
  }
}