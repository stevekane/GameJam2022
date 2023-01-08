using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Mover))]
public class Mob : MonoBehaviour {
  int Gold { get => 1 + Wave*2; }
  public int Wave = 0;

  Mover Mover;
  NavMeshAgent NavMeshAgent;

  public void DropGold() {
    var gold = (int)(Player.Get().GetComponent<Attributes>().GetValue(AttributeTag.GoldGain, Gold * Random.Range(.5f, 2f)));
    Coin.SpawnCoins(GetComponent<Defender>().LastGroundedPosition.Value.XZ() + new Vector3(0, 1f, 0), gold);
  }

  void OnDeath() {
    DropGold();
    Destroy(gameObject, .01f);
  }

  void Awake() {
    Mover = GetComponent<Mover>();
    NavMeshAgent = GetComponent<NavMeshAgent>();
    NavMeshAgent.updatePosition = false;
    NavMeshAgent.updateRotation = false;
    MobManager.Instance.Mobs.Add(this);
  }

  void OnDestroy() {
    MobManager.Instance.Mobs.Remove(this);
  }

  void FixedUpdate() {
    NavMeshAgent.nextPosition = transform.position;
    Mover.SetMove(NavMeshAgent.desiredVelocity.normalized);
  }
}
