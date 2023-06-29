using System;
using UnityEngine;

namespace Archero {
  public class Circle : MonoBehaviour {
    public TriggerEvent[] Hitboxes;
    public HitConfig HitConfig;
    Attributes Owner;
    AttributeTag EffectType;

    static public Circle Attach(Circle prefab, Attributes owner, AttributeTag type, Material material) {
      var circle = Instantiate(prefab, owner.transform.position, Quaternion.identity);
      circle.Owner = owner;
      circle.EffectType = type;
      circle.gameObject.GetComponentsInChildren<MeshRenderer>().ForEach(m => m.material = material);
      return circle;
    }

    void OnTriggerEnter(Collider other) {
      if (other.gameObject.TryGetComponent(out Hurtbox hb)) {
        var hitParams = new HitParams(HitConfig, Owner.SerializedCopy, Owner.gameObject, gameObject);
        hitParams.AttackerAttributes.AddModifier(EffectType, new() { Base = 1 });
        hb.TryAttack(hitParams);
      }
    }

    void Start() {
      Hitboxes.ForEach(hb => hb.OnTriggerEnterSource.Listen(OnTriggerEnter));
    }
    void FixedUpdate() {
      transform.position = Owner.transform.position;
    }
  }
}