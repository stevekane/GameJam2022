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
        DontDestroyOnLoad(Instance.gameObject);
      }
    }

    void OnNewRoom(Player scenePlayer) {
      GetComponent<CharacterController>().Warp(scenePlayer.transform.position, scenePlayer.transform.rotation);
      GetComponent<PersonalCamera>().Current = Camera.main;
    }
  }
}