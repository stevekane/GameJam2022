using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Archero {
  public class UpgradeUI : MonoBehaviour {
    public static UpgradeUI Instance;

    public EventSource<(Upgrades, Upgrade)> OnChoose;
    public EventSource OnReject;

    public GameObject Canvas;
    public TextMeshProUGUI Heading;
    public TextMeshProUGUI ChooseMessage;
    public GameObject ChoicesFrame;
    public UpgradeCardUI CardPrefab;
    public Button RejectButton;
    public bool IsShowing => Canvas.activeSelf;

    void Start() {
      Canvas.SetActive(false);
    }

    public void Show(Upgrades us, string heading, string chooseMessage, IEnumerable<Upgrade> choices, bool isDevil = false) {
      Heading.text = heading;
      ChooseMessage.text = chooseMessage;
      foreach (Transform child in ChoicesFrame.transform)
        Destroy(child.gameObject);
      choices.ForEach(u => {
        var card = Instantiate(CardPrefab, ChoicesFrame.transform);
        var descr = u.GetDescription(us);
        card.Init(descr);
        var b = card.GetComponent<Button>();
        b.onClick.AddListener(() => OnChooseCard(us, u));
      });
      RejectButton.gameObject.SetActive(isDevil);
      Canvas.SetActive(true);
      Invoke("FuckYouUnityYouMonumentalHeapOfFuckingGarbage", 0f);
    }

    // Setting focus *sometimes* doesn't work unless we do it in this deferred function? WTF??
    void FuckYouUnityYouMonumentalHeapOfFuckingGarbage() {
      var selected = GetComponentInChildren<Button>();
      EventSystem.current.SetSelectedGameObject(selected.gameObject);

      // Need to change InputSystem's update mode while paused or it won't update.
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
      OnChoose.Fire((us, which));
      us.BuyUpgrade(which);
      Hide();
    }
    public void OnExit() {
      OnReject.Fire();
      Hide();
    }
  }
}