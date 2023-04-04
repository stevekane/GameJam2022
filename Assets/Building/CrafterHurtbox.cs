using UnityEngine;

public class CrafterHurtbox : Hurtbox {
  [SerializeField] Crafter Machine;

  public override bool CanBeHurtBy(HitParams hitParams) {
    return (hitParams.Attacker.GetComponent<Player>() != null);
  }
  public override bool TryAttack(HitParams hitParams) {
    // TODO: fixme
    ItemFlowManager.Instance.AddCraftRequest(Machine.Recipes[0].Outputs[0].Item);
    return true;
  }
}