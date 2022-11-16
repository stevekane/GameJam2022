using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour {
  public GameObject Canvas;
  public GameObject ChoicesFrame;
  public TextMeshProUGUI GoldText;
  public UpgradeCardUI CardPrefab;
  public bool IsShowing { get; private set; }
  Upgrade[] UpgradeChoices;

  void Start() {
    Canvas.SetActive(false);
  }

  public void Show(Upgrade[] choices) {
    UpgradeChoices = choices;

    foreach (Transform child in ChoicesFrame.transform)
      Destroy(child.gameObject);
    Optional<Button> selected = GetComponentInChildren<Button>();
    var playerUs = Player.Get().GetComponent<Upgrades>();
    GoldText.text = $"Gold: ${playerUs.Gold}";
    choices.ForEach(u => {
      var card = Instantiate(CardPrefab, ChoicesFrame.transform);
      var descr = u.GetDescription(playerUs);
      card.Init(descr);
      var b = card.GetComponent<Button>();
      if (descr.Cost <= playerUs.Gold) {
        selected ??= b;
        b.onClick.AddListener(() => OnChooseCard(u));
      } else {
        b.interactable = false;
      }
    });
    Canvas.SetActive(true);
    InputManager.Instance.SetInputEnabled(false);
    Time.timeScale = 0f;
    if (selected != null)
      EventSystem.current.SetSelectedGameObject(selected.Value.gameObject);

    IsShowing = true;
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
    IsShowing = false;
  }

  public void OnChooseCard(Upgrade which) {
    Player.Get().GetComponent<Upgrades>().BuyUpgrade(which);
    Show(UpgradeChoices);
  }
  public void OnExit() {
    Hide();
  }
}
