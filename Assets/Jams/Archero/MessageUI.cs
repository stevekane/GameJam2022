using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Archero {
  public class MessageUI : MonoBehaviour {
    public static MessageUI Instance;

    public GameObject Canvas;
    public TextMeshProUGUI Message;
    public Button Accept;
    public TextMeshProUGUI AcceptText;
    public bool IsShowing => Canvas.activeSelf;

    void Start() {
      Canvas.SetActive(false);
    }

    public void Show(string message, string acceptText) {
      Message.text = message;
      AcceptText.text = acceptText;
      Canvas.SetActive(true);
      Invoke("FuckYouUnityYouMonumentalHeapOfFuckingGarbage", 0f);
    }

    // Setting focus *sometimes* doesn't work unless we do it in this deferred function? WTF??
    void FuckYouUnityYouMonumentalHeapOfFuckingGarbage() {
      EventSystem.current.SetSelectedGameObject(Accept.gameObject);

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

    public void OnExit() {
      Hide();
    }
  }
}