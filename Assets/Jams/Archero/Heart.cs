using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Heart : MonoBehaviour {
    [SerializeField] Collider CollectionTrigger;
    [SerializeField] Vector3 BurstForce = new Vector3(5, 5, 5);
    [SerializeField] float DropRate = .02f;
    [SerializeField] float HealFraction = .02f;
    [SerializeField] float CollectSpeed = 40f;

    Rigidbody Rigidbody;

    public static void MaybeSpawn(Vector3 position) {
      var playerAt = Player.Instance.GetComponent<Attributes>();
      var dropRate = playerAt.GetValue(AttributeTag.StrongHeart, GameManager.Instance.HeartPrefab.DropRate);
      var roll = dropRate > Random.Range(0, 1f);
      // Debug.Log($"Drop rate {dropRate} => {roll}");
      if (roll)
        Instantiate(GameManager.Instance.HeartPrefab, position, Quaternion.identity);
    }

    void OnEnable() {
      var impulse = Vector3.Scale(BurstForce, Random.onUnitSphere) * Random.Range(.5f, 1f);
      impulse.y = Mathf.Abs(impulse.y);
      Rigidbody.AddForce(impulse, ForceMode.Impulse);
      Rigidbody.isKinematic = false;
      CollectionTrigger.enabled = false;
    }

    void OnDisable() {
      Rigidbody.isKinematic = false;
      CollectionTrigger.enabled = true;
    }

    public async Task Collect(TaskScope scope) {
      Rigidbody.isKinematic = true;
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

    void OnTriggerEnter(Collider other) {
      if (other.GetComponent<Player>() && other.TryGetComponent(out Damageable d)) {
        var frac = d.GetComponent<Attributes>().GetValue(AttributeTag.StrongHeart, HealFraction);
        // Debug.Log($"Would heal for {frac}");
        d.Heal(Mathf.RoundToInt(frac * d.MaxHealth));
        Destroy(gameObject);
      }
    }

    void Awake() {
      this.InitComponent(out Rigidbody);
    }
  }
}