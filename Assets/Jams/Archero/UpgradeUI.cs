using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Archero {
  public class UpgradeUI : MonoBehaviour {
    public static UpgradeUI Instance;

    public GameObject Canvas;
    public GameObject ChoicesFrame;
    public UpgradeCardUI CardPrefab;
    public TextMeshProUGUI LevelText;
    public bool IsShowing { get; private set; }

    void Start() {
      Canvas.SetActive(false);
    }

    public void Show(Upgrades us, Upgrade[] choices) {
      LevelText.text = $"Level {us.CurrentLevel} in this adventure!";
      foreach (Transform child in ChoicesFrame.transform)
        Destroy(child.gameObject);
      choices.ForEach(u => {
        var card = Instantiate(CardPrefab, ChoicesFrame.transform);
        var descr = u.GetDescription(us);
        card.Init(descr);
        var b = card.GetComponent<Button>();
        b.onClick.AddListener(() => OnChooseCard(us, u));
      });
      Canvas.SetActive(true);
      Invoke("FuckYouUnityYouMonumentalHeapOfFuckingGarbage", 0f);

      IsShowing = true;
    }

    // Setting focus *sometimes* doesn't work unless we do it in this deferred function? WTF??
    void FuckYouUnityYouMonumentalHeapOfFuckingGarbage() {
      var selected = GetComponentInChildren<Button>();
      EventSystem.current.SetSelectedGameObject(selected.gameObject);

      InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
      Time.timeScale = 0f;
    }

    public void Hide() {
      EventSystem.current.SetSelectedGameObject(null);
      Canvas.SetActive(false);
      Time.timeScale = 1f;
      InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
    }

    public void OnChooseCard(Upgrades us, Upgrade which) {
      us.BuyUpgrade(which);
      Hide();
    }
    public void OnExit() {
      Hide();
    }
  }
}