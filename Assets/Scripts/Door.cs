using UnityEngine;

public class Door : MonoBehaviour {
  [SerializeField] public Room ConnectingRoom;
  private void OnTriggerEnter(Collider other) {
    // var scene = GameObject.FindObjectOfType<SceneManager>();
    // scene.EnterRoom(ConnectingRoom);
  }

  //public void EnterRoom(Room room) {
  //  var oldRoomPrefab = RoomPrefab;

  //  Destroy(Room.gameObject);
  //  RoomPrefab = room;
  //  Room = Instantiate(RoomPrefab, transform);

  //  // Put the player in front of the door that connects to our old room.
  //  var matchingDoor = room.GetComponentsInChildren<Door>().FirstOrDefault((Door d) => d.ConnectingRoom == oldRoomPrefab);
  //  Debug.Assert(matchingDoor != null);
  //  var player = GameObject.FindObjectOfType<Player>();
  //  player.OnRoomEntered(room, matchingDoor);
  //}

}