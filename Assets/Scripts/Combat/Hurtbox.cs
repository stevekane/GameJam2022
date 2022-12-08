using UnityEngine;

public class Hurtbox : MonoBehaviour {
  public GameObject Owner;
  public EventSource<HitParams> OnHurt = new();

  void Awake() {
    Owner = Owner ?? transform.parent.gameObject;
  }

  public void TryAttack(HitParams hitParams) {
    if (Owner.TryGetComponent(out Attributes defenderAttributes))
      hitParams.DefenderAttributes = defenderAttributes.serialized;
    hitParams.Defender = Owner;
    hitParams.Defender.SendMessage("OnHurt", hitParams, SendMessageOptions.DontRequireReceiver);
    hitParams.Source.SendMessage("OnHit", hitParams, SendMessageOptions.DontRequireReceiver);
    OnHurt.Fire(hitParams);
    CameraShaker.Instance.Shake(hitParams.HitConfig.CameraShakeStrength);
  }
}