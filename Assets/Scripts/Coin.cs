using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour {
  public float BurstSpeed = 10f;
  public float CollectSpeed = 10f;
  public static void SpawnCoins(Vector3 position, int amount) {
    for (int i = 0; i < amount; i++) {
      Instantiate(VFXManager.Instance.CoinPrefab, position, Quaternion.identity);
    }
  }

  void Start() {
    StartCoroutine(Routine());
  }

  IEnumerator Routine() {
    yield return StartCoroutine(Burst());
    yield return StartCoroutine(Collect());
  }

  IEnumerator Burst() {
    var gravity = -200f;
    var speed = BurstSpeed;
    var velocity = new Vector3(Random.Range(-1f, 1f), 3f, Random.Range(-1f, 1f)).normalized * speed;
    while (velocity.y > 0f || transform.position.y > .01f) {
      velocity.y += Time.fixedDeltaTime * gravity;
      transform.position += Time.fixedDeltaTime * velocity;
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator Collect() {
    yield return new WaitForSeconds(2f);
    var player = FindObjectOfType<Player>();
    var speed = CollectSpeed;
    var accel = 60f;
    while (true) {
      var dir = (player.transform.position - transform.position).normalized;
      speed += Time.fixedDeltaTime * accel;
      transform.position += Time.fixedDeltaTime * speed * dir;
      yield return new WaitForFixedUpdate();
    }
  }

  void FixedUpdate() {
    var player = FindObjectOfType<Player>();
    if ((player.transform.position - transform.position).sqrMagnitude < 10f) {
      player.GetComponent<Upgrades>().CollectGold(1);
      Destroy(gameObject);
    }
  }
}
