using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour {
  public GameObject Canvas;
  public GameObject ChoicesFrame;
  public UpgradeCardUI CardPrefab;
  void Start() {
    Canvas.SetActive(false);
  }

  public void Show(Upgrade[] choices) {
    foreach (Transform child in ChoicesFrame.transform)
      Destroy(child.gameObject);
    Optional<GameObject> selected = null;
    var playerUs = Player.Get().GetComponent<Upgrades>();
    choices.ForEach(u => {
      var card = Instantiate(CardPrefab, ChoicesFrame.transform);
      var descr = u.GetDescription(playerUs);
      card.Init(descr);
      var b = card.GetComponent<Button>();
      if (descr.Cost <= playerUs.Gold) {
        selected ??= card.gameObject;
        b.onClick.AddListener(() => OnChooseCard(u));
      } else {
        b.interactable = false;
      }
    });
    Canvas.SetActive(true);
    InputManager.Instance.SetInputEnabled(false);
    Time.timeScale = 0f;
    if (selected != null)
      EventSystem.current.SetSelectedGameObject(selected.Value);
  }

  public void Hide() {
    Canvas.SetActive(false);
    InputManager.Instance.SetInputEnabled(true);
    Time.timeScale = 1f;
  }

  public void OnChooseCard(Upgrade which) {
    which.Buy(Player.Get().GetComponent<Upgrades>());
    Hide();
  }
  public void OnExit() {
    Hide();
  }
}
