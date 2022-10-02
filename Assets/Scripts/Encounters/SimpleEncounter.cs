using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEncounter : MonoBehaviour {
  public List<GameObject> Mobs;
  public List<Spawn> Spawns;
  public float Period;

  bool Triggered = false;

  void OnTriggerEnter(Collider c) {
    if (Triggered || !c.TryGetComponent(out Player player)) {
      return;
    }

    StartCoroutine(MakeRoutine());
    Triggered = true;
  }

  IEnumerator MakeRoutine() {
    var i = 0;
    var j = 0;
    while (true) {
      yield return new WaitForSeconds(Period);
      var p = Spawns[i].transform;
      var m = Instantiate(Mobs[j], p.position, p.rotation);
      i = (i+1)%Spawns.Count;
      j = (j+1)%Mobs.Count;
    }
  }
}