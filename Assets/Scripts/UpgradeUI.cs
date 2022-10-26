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
    Time.timeScale = 1f;
    // TODO: This is a dumb hack so the button onRelease doesn't register as a player input.
    // Should probably have a close-shop animation anyway.
    Invoke("EnableInput", .1f);
  }

  void EnableInput() {
    InputManager.Instance.SetInputEnabled(true);
  }

  public void OnChooseCard(Upgrade which) {
    which.Buy(Player.Get().GetComponent<Upgrades>());
    Hide();
  }
  public void OnExit() {
    Hide();
  }
}
