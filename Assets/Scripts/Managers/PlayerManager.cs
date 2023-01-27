using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour {
  public static PlayerManager Instance;

  PlayerInputActions Controls;
  Dictionary<Player, InputDevice> PlayerGamepads = new();

  void Awake() {
    Controls = new();
    Controls.Player.Start.performed += PlayerPressedStart;
  }
  void OnEnable() => Controls.Enable();
  void OnDisable() => Controls.Disable();

  public void RegisterPlayer(Player player) {
    if (PlayerGamepads.Count == 0) {
      // Special case - first player doesn't need to press Start.
      InitPlayer(player, Gamepad.all.FirstOrDefault(g => g.name.Contains("DualShock")));
    }
    // Otherwise, PlayerPressedStart will call InitPlayer with the proper device.
  }
  public void UnregisterPlayer(Player player) {
    PlayerGamepads.Remove(player);
  }

  void PlayerPressedStart(InputAction.CallbackContext ctx) {
    if (!PlayerGamepads.ContainsValue(ctx.control.device)) {
      var player = GameManager.Instance.SpawnPlayer();
      InitPlayer(player, ctx.control.device);
    }
  }

  void InitPlayer(Player player, InputDevice device) {
    int teamID = PlayerGamepads.Count;
    player.GetComponent<Team>().ID = teamID;
    if (device != null) {
      // Special case - first player gets mouse/keyboard.
      player.GetComponent<InputManager>().AssignDevices(
        teamID == 0 ? new InputDevice[] { device, Keyboard.current, Mouse.current } : new InputDevice[] { device });
    }
    PlayerGamepads[player] = device;
  }
}