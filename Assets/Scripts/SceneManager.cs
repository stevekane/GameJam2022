using UnityEngine;
using System.Linq;

// Create instance of Application-wide Game object if not found
public class SceneManager : MonoBehaviour {
  [HideInInspector] public Game Game;
  [HideInInspector] public Room RoomPrefab;  // used to link Doors together
  [HideInInspector] public Room Room;
  [SerializeField] private Game GamePrefab;
  [SerializeField] private Room StartingRoom;

  public void Awake() {
    var includeInactive = false;
    var game = GameObject.FindObjectOfType<Game>(includeInactive);
    if (game) {
      Game = game;
    } else {
      Game = Instantiate(GamePrefab, transform);
    }

    RoomPrefab = StartingRoom;
    Room = Instantiate(RoomPrefab, transform);
  }

  public void OnDestroy() {
    Destroy(Game);
    Game = null;
  }

  public void EnterRoom(Room room) {
    var oldRoomPrefab = RoomPrefab;

    Destroy(Room.gameObject);
    RoomPrefab = room;
    Room = Instantiate(RoomPrefab, transform);

    // Put the player in front of the door that connects to our old room.
    var matchingDoor = room.GetComponentsInChildren<Door>().FirstOrDefault((Door d) => d.ConnectingRoom == oldRoomPrefab);
    Debug.Assert(matchingDoor != null);
    var player = GameObject.FindObjectOfType<Player>();
    player.OnRoomEntered(room, matchingDoor);
  }
}