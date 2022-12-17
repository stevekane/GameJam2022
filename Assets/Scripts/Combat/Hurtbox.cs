using UnityEngine;

public class Hurtbox : MonoBehaviour {
  public GameObject Owner;
  public EventSource<HitParams> OnHurt = new();

  void Awake() {
    Owner = Owner ?? transform.parent.gameObject;
  }

  public void TryAttack(HitParams hitParams) {
    hitParams.Defender = Owner;
    if (Owner.TryGetComponent(out Attributes defenderAttributes))
      hitParams.DefenderAttributes = defenderAttributes.serialized;
    if (Owner.TryGetComponent(out Status status)) {
      if (status.IsHittable) {
        hitParams.Defender.SendMessage("OnHurt", hitParams, SendMessageOptions.DontRequireReceiver);
        hitParams.Source.SendMessage("OnHit", hitParams, SendMessageOptions.DontRequireReceiver);
        OnHurt.Fire(hitParams);
        CameraShaker.Instance.Shake(hitParams.HitConfig.CameraShakeStrength);
      }
    } else {
      hitParams.Defender.SendMessage("OnHurt", hitParams, SendMessageOptions.DontRequireReceiver);
      hitParams.Source.SendMessage("OnHit", hitParams, SendMessageOptions.DontRequireReceiver);
      OnHurt.Fire(hitParams);
      CameraShaker.Instance.Shake(hitParams.HitConfig.CameraShakeStrength);
    }
  }
}