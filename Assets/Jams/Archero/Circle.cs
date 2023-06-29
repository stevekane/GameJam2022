using System;
using System.Linq;
using UnityEngine;

namespace Archero {
  public class Circle : MonoBehaviour {
    public TriggerEvent[] Hitboxes;
    public HitConfig HitConfig;
    Attributes Owner;
    AttributeTag EffectType;

    static public Circle Attach(Circle prefab, Attributes owner, AttributeTag type, Material material) {
      var otherCircles = FindObjectsOfType<Circle>().Where(c => c.Owner == owner).ToArray();
      var numCircles = otherCircles.Length;
      var offsetY = numCircles switch {
        0 => 0f,
        1 => 90f,
        2 => 45f,
        3 => -90f,
        _ => 0f,  // 4 is possible but unlikely so fuck it
      };
      if (numCircles > 0) offsetY += otherCircles[0].transform.rotation.eulerAngles.y;  // 0 seems to be the most recent one?
      Debug.Log($"Circle: {numCircles} existing, angleY={offsetY}");
      var circle = Instantiate(prefab, owner.transform.position, Quaternion.Euler(0, offsetY, 0));
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