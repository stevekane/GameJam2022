using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour {
  public float BurstForce = 10f;
  public float Gravity = -200f;
  public float CollectSpeed = 40f;

  public static void SpawnCoins(Vector3 position, int amount) {
    for (int i = 0; i < amount; i++) {
      Instantiate(VFXManager.Instance.CoinPrefab, position, Quaternion.identity);
    }
  }

  void Start() {
    StartCoroutine(Routine());
  }

  IEnumerator Routine() {
    StartCoroutine(Burst()); // don't wait for the burst
    yield return new WaitForSeconds(2f);
    yield return StartCoroutine(Collect());
  }

  IEnumerator Burst() {
    var rb = GetComponent<Rigidbody>();
    var impulse = new Vector3(Random.Range(-1f, 1f), 5f, Random.Range(-1f, 1f)).normalized * BurstForce;
    rb.AddForce(impulse, ForceMode.Impulse);
    yield return new WaitForFixedUpdate();
    var velocity = rb.velocity;
    // Why do I have to manually simulate gravity? AddForce does not work right
    while (velocity.y > 0f || transform.position.y > .01f) {
      velocity.y += Time.fixedDeltaTime * Gravity;
      rb.MovePosition(transform.position + Time.fixedDeltaTime * velocity);
      yield return new WaitForFixedUpdate();
    }
    rb.isKinematic = true;
  }

  IEnumerator Collect() {
    var player = Player.Get();
    var accel = 60f;
    while (player) {
      var dir = (player.transform.position - transform.position).normalized;
      CollectSpeed += Time.fixedDeltaTime * accel;
      transform.position += Time.fixedDeltaTime * CollectSpeed * dir;
      yield return new WaitForFixedUpdate();
    }
  }

  void OnTriggerEnter(Collider other) {
    if (other.GetComponent<Player>() && other.TryGetComponent(out Upgrades us)) {
      us.CollectGold(1);
      Destroy(gameObject);
    }
  }

}
