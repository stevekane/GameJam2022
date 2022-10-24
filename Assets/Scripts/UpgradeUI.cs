using UnityEngine;

public class UpgradeUI : MonoBehaviour {
  public GameObject Canvas;
  void Start() {
    Canvas.SetActive(false);
  }

  public void Show() {
    Canvas.SetActive(true);
    InputManager.Instance.SetInputEnabled(false);
    Time.timeScale = 0f;
  }

  public void Hide() {
    Canvas.SetActive(false);
    InputManager.Instance.SetInputEnabled(true);
    Time.timeScale = 1f;
  }

  public void OnChooseCard(int which) {
    Debug.Log($"Player chose card {which}");
    Hide();
  }
}
