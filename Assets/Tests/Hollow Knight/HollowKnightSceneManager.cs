using System.Collections;
using UnityEngine;

public class HollowKnightSceneManager : MonoBehaviour {
  public HollowKnight PlayerPrefab;
  public Transform PlayerSpawnPoint;

  IEnumerator Routine;
  HollowKnight Player;

  void Awake() => Routine = new Fiber(MakeRoutine());
  void FixedUpdate() => Routine.MoveNext();
  void OnDestroy() => Routine = null;

  IEnumerator MakeRoutine() {
    while (true) {
      yield return Fiber.Until(() => Player == null);
      Player = Instantiate(PlayerPrefab, PlayerSpawnPoint.position, PlayerSpawnPoint.rotation);
    }
  }
}