using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archero {
  public class UpgradeUI : MonoBehaviour {
    public static UpgradeUI Instance;

    public GameObject Canvas;
    public GameObject ChoicesFrame;
    public UpgradeCardUI CardPrefab;
    public TextMeshProUGUI LevelText;
    public bool IsShowing { get; private set; }
    Upgrade[] UpgradeChoices;

    void Start() {
      Archero.UpgradeUI.Instance = this;
      Canvas.SetActive(false);
    }

    public void Show(Upgrade[] choices) {
      UpgradeChoices = choices;

      var level = 42;
      LevelText.text = $"Level {level} in this adventure!";
      foreach (Transform child in ChoicesFrame.transform)
        Destroy(child.gameObject);
      Button selected = GetComponentInChildren<Button>();
      var playerUs = Player.Get().GetComponent<Upgrades>();
      choices.ForEach(u => {
        var card = Instantiate(CardPrefab, ChoicesFrame.transform);
        var descr = u.GetDescription(playerUs);
        card.Init(descr);
        var b = card.GetComponent<Button>();
        b.onClick.AddListener(() => OnChooseCard(u));
      });
      Canvas.SetActive(true);
      Player.Get().GetComponent<InputManager>().SetInputEnabled(false);
      Time.timeScale = 0f;
      if (selected != null)
        EventSystem.current.SetSelectedGameObject(selected.gameObject);

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
      //Player.Get().GetComponent<InputManager>().SetInputEnabled(true);
      IsShowing = false;
    }

    public void OnChooseCard(Upgrade which) {
      //Player.Get().GetComponent<Upgrades>().BuyUpgrade(which);
      Show(UpgradeChoices);
    }
    public void OnExit() {
      Hide();
    }
  }
}