using System.Collections;
using UnityEngine;

public class Missile : MonoBehaviour {
  public GameObject PayloadPrefab;
  public Timeval Duration = Timeval.FromSeconds(1);
  public Vector3 Target;

  IEnumerator Start() {
    yield return Fiber.Wait(Duration);
    Instantiate(PayloadPrefab, Target, Quaternion.identity);
    Destroy(gameObject);
  }
}