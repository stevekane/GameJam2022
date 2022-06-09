using UnityEngine;

public class Door : MonoBehaviour {
  [SerializeField] public Room ConnectingRoom;
  private void OnTriggerEnter(Collider other) {
    // var scene = GameObject.FindObjectOfType<SceneManager>();
    // scene.EnterRoom(ConnectingRoom);
  }
}