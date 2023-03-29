using UnityEngine;

namespace ActionsAndAI {
  public class Fire : AbstractActionBehavior {
    [SerializeField] Transform Origin;
    [SerializeField] GameObject BulletPrefab;
    public bool IsActive;
    public override bool CanStart() => IsActive;
    public override void OnStart() {
      var bullet = GameObject.Instantiate(BulletPrefab, Origin.position, Origin.rotation);
      bullet.gameObject.SetActive(true);
      bullet.GetComponent<Rigidbody>().AddForce(Origin.forward * 10, ForceMode.Impulse);
    }
  }
}