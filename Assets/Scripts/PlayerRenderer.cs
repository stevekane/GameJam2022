using UnityEngine;

public class PlayerRenderer : MonoBehaviour {
  public GameObject MovingMesh;
  public GameObject RollingMesh;
  public GameObject SpinningMesh;
  public Player Player;

  public void Update() {
    switch (Player.State) {
      case Player.PlayerState.Moving: {
        MovingMesh.SetActive(true);
        RollingMesh.SetActive(false);
        SpinningMesh.SetActive(false);
      }
      break;

      case Player.PlayerState.Rolling: {
        MovingMesh.SetActive(false);
        RollingMesh.SetActive(true);
        SpinningMesh.SetActive(false);
      }
      break;

      case Player.PlayerState.Spinning: {
        MovingMesh.SetActive(true);
        RollingMesh.SetActive(false);
        SpinningMesh.SetActive(true);
      }
      break;
    }
  }
}