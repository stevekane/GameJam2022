using System.Collections;
using UnityEngine;

public class Missile : MonoBehaviour {
  public GameObject PayloadPrefab;
  public Timeval Duration = Timeval.FromSeconds(1);
  public Vector3 Target;
  public HitParams HitParams { get; set; }

  IEnumerator Start() {
    yield return Fiber.Wait(Duration);
    var payload = Instantiate(PayloadPrefab, Target, Quaternion.identity);
    payload.GetComponent<TargetedStrike>().HitParams = HitParams;
    Destroy(gameObject);
  }
}