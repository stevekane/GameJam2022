using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
  public static IEnumerator Await(AsyncOperation op) {
    while (!op.isDone) {
      yield return op;
    }
  }

  public GameObject PlayerPrefab;

  GameObject Player;
  PlayerSpawn[] PlayerSpawns;
  MobSpawn[] MobSpawns;

  IEnumerator Start() {
    PlayerSpawns = FindObjectsOfType<PlayerSpawn>();
    MobSpawns = FindObjectsOfType<MobSpawn>();
    while (true) {
      // Spawn and configure the player
      var playerSpawn = PlayerSpawns[0];
      Instantiate(PlayerPrefab, playerSpawn.transform.position, playerSpawn.transform.rotation);

      Debug.Log("Waitin for spacebar...");
      yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
      Debug.Log("Got a spacebar...");

      // Cleanup managers and reload the scene
      yield return Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name));
    }
  }
}