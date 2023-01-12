using UnityEngine;

public class Parrybox : MonoBehaviour {
  public GameObject Owner;
  public Team Team;
  public EventSource<HitParams> OnDidParry = new();
  Collider Collider;

  public bool EnableCollision {
    get => Collider.enabled;
    set => Collider.enabled = value;
  }

  void Awake() {
    this.InitComponent(out Collider);
    Owner = Owner ?? transform.parent.gameObject;
    Team = Team ?? Owner.GetComponent<Team>();
  }

  public bool CanBeHurtBy(HitParams hitParams) {
    if (!Team.CanBeHurtBy(hitParams.AttackerTeamID))
      return false;
    return true;
  }
  public bool TryParry(HitParams hitParams) {
    if (!CanBeHurtBy(hitParams)) return false;

    hitParams.Defender = Owner;
    if (Owner.TryGetComponent(out Attributes defenderAttributes))
      hitParams.DefenderAttributes = defenderAttributes;
    hitParams.Source.SendMessage("OnWasParried", hitParams, SendMessageOptions.DontRequireReceiver);
    hitParams.Defender.SendMessage("OnDidParry", hitParams, SendMessageOptions.DontRequireReceiver);
    OnDidParry.Fire(hitParams);

    return true;
  }
}