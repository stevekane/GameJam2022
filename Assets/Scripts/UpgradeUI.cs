using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour {
  public GameObject Canvas;
  public GameObject ChoicesFrame;
  public GameObject ChoicePrefab;
  void Start() {
    Canvas.SetActive(false);
  }

  public void Show(Upgrade[] choices) {
    foreach (Transform child in ChoicesFrame.transform)
      Destroy(child.gameObject);
    choices.ForEach(c => {
      var go = Instantiate(ChoicePrefab, ChoicesFrame.transform);
      var b = go.GetComponent<Button>();
      b.onClick.AddListener(() => OnChooseCard(c));
    });
    Canvas.SetActive(true);
    InputManager.Instance.SetInputEnabled(false);
    Time.timeScale = 0f;
    EventSystem.current.SetSelectedGameObject(ChoicesFrame.transform.GetChild(0).gameObject);
  }

  public void Hide() {
    Canvas.SetActive(false);
    InputManager.Instance.SetInputEnabled(true);
    Time.timeScale = 1f;
  }

  public void OnChooseCard(Upgrade which) {
    Debug.Log($"Player chose card {which}");
    Hide();
  }
}
