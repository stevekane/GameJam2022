using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Coin : MonoBehaviour {
    public float BurstForce = 10f;
    public float Gravity = -200f;
    public float CollectSpeed = 40f;

    static GameObject _Parent;
    static GameObject Parent => _Parent = _Parent ? _Parent : new GameObject("Coins");

    public static void SpawnCoins(Vector3 position, int amount) {
      for (int i = 0; i < amount; i++) {
        var c = Instantiate(GameManager.Instance.CoinPrefab, position, Quaternion.identity);
        c.transform.SetParent(Parent.transform, true);
      }
    }

    void Start() {
      StartCoroutine(Burst());
    }

    IEnumerator Burst() {
      var rb = GetComponent<Rigidbody>();
      var xz = UnityEngine.Random.onUnitSphere;
      var impulse = new Vector3(xz.x, 3f, xz.z).normalized * BurstForce;
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

    public void Collect() {
      StartCoroutine(CollectRoutine());
    }
    public async Task Collect(TaskScope scope) {
      GetComponent<Collider>().enabled = true;
      var player = Player.Instance;
      var accel = 60f;
      while (player && this) {
        CollectSpeed += Time.fixedDeltaTime * accel;
        var delta = player.transform.position - transform.position;
        var dist = Mathf.Min(Time.fixedDeltaTime * CollectSpeed, delta.magnitude);
        transform.position += dist * delta.normalized;
        await scope.TickTime();
      }
    }
    IEnumerator CollectRoutine() {
      GetComponent<Collider>().enabled = true;
      var player = Player.Instance;
      var accel = 60f;
      while (player && this) {
        CollectSpeed += Time.fixedDeltaTime * accel;
        var delta = player.transform.position - transform.position;
        var dist = Mathf.Min(Time.fixedDeltaTime * CollectSpeed, delta.magnitude);
        transform.position += dist * delta.normalized;
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
}