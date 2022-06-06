using UnityEngine;

public class Player : Hero {
  public void OnRoomEntered(Room room, Door startingDoor) {
    //CharacterController.enabled = false;  // Quick hack until we have a real transition state.
    transform.position = startingDoor.transform.position - startingDoor.transform.position.normalized*2;
    //CharacterController.enabled = true;
  }
}