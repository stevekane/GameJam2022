using System.Collections.Generic;
using TMPro;
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
    Optional<GameObject> selected = null;
    var playerUs = Player.Get().GetComponent<Upgrades>();
    choices.ForEach(c => {
      var go = Instantiate(ChoicePrefab, ChoicesFrame.transform);
      selected ??= go;
      var b = go.GetComponent<Button>();
      b.onClick.AddListener(() => OnChooseCard(c));
      var txt = go.GetComponentInChildren<TextMeshProUGUI>();
      txt.text = c.GetDescription(playerUs);
    });
    Canvas.SetActive(true);
    InputManager.Instance.SetInputEnabled(false);
    Time.timeScale = 0f;
    EventSystem.current.SetSelectedGameObject(selected.Value);
  }

  public void Hide() {
    Canvas.SetActive(false);
    InputManager.Instance.SetInputEnabled(true);
    Time.timeScale = 1f;
  }

  public void OnChooseCard(Upgrade which) {
    which.Add(Player.Get().GetComponent<Upgrades>());
    Hide();
  }
}
