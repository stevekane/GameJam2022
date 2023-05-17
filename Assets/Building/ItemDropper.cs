using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemDropper : MonoBehaviour {
  [Serializable]
  public class DropChance {
    public float Chance;
    public ItemInfo Item;
  }

  public List<DropChance> Drops;
  public float BurstForce = 40f;

  public void Drop() {
    var roll = UnityEngine.Random.Range(0, 1f);
    var drops = Drops.Where(d => roll < d.Chance);
    var pos = transform.position;
    foreach (var d in drops) {
      var obj = d.Item.Spawn(pos);
      GameManager.Instance.GlobalScope.Start(async s => {
        Burst(obj.GetComponent<Rigidbody>());
        await s.Seconds(1f);
        obj?.MakePickupable();
      });
    }
  }

  void Burst(Rigidbody rb) {
    rb.isKinematic = false;
    rb.useGravity = false;
    var impulse = new Vector3(UnityEngine.Random.Range(-1f, 1f), 5f, UnityEngine.Random.Range(-1f, 1f)).normalized * BurstForce;
    rb.AddForce(impulse, ForceMode.Impulse);
    rb.gameObject.AddComponent<ConstantForce>().force = new(0, -50f, 0f);
  }

  void OnDeath(Vector3 normal) {
    Drop();
  }
}