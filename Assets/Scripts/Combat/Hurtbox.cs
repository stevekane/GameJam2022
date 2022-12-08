using UnityEngine;

public class Hurtbox : MonoBehaviour {
  public GameObject Owner;
  public EventSource<HitParams> OnHurt = new();

  void Awake() {
    Owner = Owner ?? transform.parent.gameObject;
  }

  public void TryAttack(HitParams hitParams) {
    hitParams.Defender = Owner;
    OnHurt.Fire(hitParams);
    Owner.SendMessage("OnHurt", hitParams, SendMessageOptions.DontRequireReceiver);
  }
}