using UnityEngine;

public class Hurtbox : MonoBehaviour {
  public GameObject Owner;
  public EventSource<HitParams> OnHit = new();

  void Awake() {
    Owner = Owner ?? transform.parent.gameObject;
  }

  public void TryAttack(Attributes attacker, HitConfig config) {
    var hitParams = config.ComputeParams(attacker, this);
    OnHit.Fire(hitParams);
    Owner.SendMessage("OnHit", hitParams, SendMessageOptions.DontRequireReceiver);
  }
}