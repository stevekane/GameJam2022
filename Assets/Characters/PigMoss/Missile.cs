using System.Collections;
using UnityEngine;

public class Missile : MonoBehaviour {
  public GameObject PayloadPrefab;
  public Timeval Duration = Timeval.FromSeconds(1);
  public Vector3 Target;

  IEnumerator Start() {
    for (var i = 0; i < Duration.Ticks; i++) {
      yield return new WaitForFixedUpdate();
    }
    var payload = Instantiate(PayloadPrefab, Target, Quaternion.identity);
    Destroy(gameObject);
  }
}