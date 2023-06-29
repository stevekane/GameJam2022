using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Coin : MonoBehaviour {
    [SerializeField] Rigidbody Rigidbody;
    [SerializeField] Collider CollectionTrigger;
    [SerializeField] Vector3 BurstForce = new Vector3(10, 1, 10);
    [SerializeField] float CollectSpeed = 40f;

    static GameObject _Parent;
    static GameObject Parent => _Parent = _Parent ? _Parent : new GameObject("Coins");

    public static void SpawnCoins(Vector3 position, int amount) {
      for (int i = 0; i < amount; i++) {
        var c = Instantiate(GameManager.Instance.CoinPrefab, position, Quaternion.identity);
        c.transform.SetParent(Parent.transform, true);
      }
    }

    void Start() {
      Rigidbody.AddForce(Vector3.Scale(BurstForce, Random.onUnitSphere), ForceMode.Impulse);
    }

    public void Collect() {
      StartCoroutine(CollectRoutine());
    }

    public async Task Collect(TaskScope scope) {
      CollectionTrigger.enabled = true;
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
      Rigidbody.isKinematic = true;
      CollectionTrigger.enabled = true;
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