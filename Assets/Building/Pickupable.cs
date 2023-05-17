using System.Threading.Tasks;
using UnityEngine;

public class Pickupable : MonoBehaviour {
  public ItemObject ItemObject { get; set; }

  TaskScope PickupTask;
  void OnTriggerStay(Collider other) {
    if (PickupTask == null && other.TryGetComponent(out Pickupper pickupper))
      PickupTask = TaskScope.StartNew(s => Pickup(s, pickupper));
  }

  async Task Pickup(TaskScope scope, Pickupper pickupper) {
    GetComponent<Collider>().enabled = false;
    var speed = 10f;
    var accel = 20f;
    var rb = GetComponent<Rigidbody>();
    rb.isKinematic = true;
    rb.velocity = Vector3.zero;
    while (true) {
      var delta = pickupper.transform.position - transform.position;
      if (delta.sqrMagnitude < speed * Time.fixedDeltaTime)
        break;
      transform.position += speed * Time.fixedDeltaTime * delta.normalized;
      speed += accel * Time.fixedDeltaTime;
      await scope.Tick();
    }
    pickupper.Pickup(ItemObject);
    ItemObject.gameObject.Destroy();
  }

  void OnDestroy() {
    PickupTask?.Dispose();
  }
}