using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VictoryPickupper : MonoBehaviour {
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out VictoryPickup vp)) {
      StartCoroutine(Shrink(vp.transform.parent));
    }
  }

  public float Speed = 1f;
  public float MaxSize = 100f;
  public GameObject Fireworks;
  private IEnumerator Shrink(Transform obj) {
    var size = obj.localScale.x;
    while (size < MaxSize) {
      obj.localScale = new Vector3(size, size, size);
      size = size + Speed*Time.fixedDeltaTime;
      yield return null;
    }
    VFXManager.Instance.TrySpawnEffect(Fireworks, obj.position, obj.transform.rotation, 5f);
    obj.gameObject.Destroy();
  }
}