using UnityEngine;

namespace Archero {
  public class Player : MonoBehaviour {
    public static Player Instance;

    void Awake() {
      if (Instance) {
        Instance.OnNewRoom(this);
        Destroy(gameObject);
      } else {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(Instance.gameObject);
      }
    }

    void OnNewRoom(Player scenePlayer) {
      GetComponent<Upgrades>().OnNewRoom();
      GetComponent<CharacterController>().Warp(scenePlayer.transform.position, scenePlayer.transform.rotation);
      GetComponent<PersonalCamera>().Current = Camera.main;
    }

    void OnDeath() {
      GameManager.Instance.OnPlayerDied();
    }
  }
}