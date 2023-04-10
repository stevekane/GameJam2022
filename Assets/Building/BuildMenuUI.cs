using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildMenuUI : MonoBehaviour {
  public GameObject Canvas;
  public BuildMenuItemUI CardPrefab;
  public bool IsShowing { get; private set; }
  public float Radius = 100f;

  void Start() {
    Canvas.SetActive(false);
  }

  public void Show(string[] choices) {
    //RectTransform rectTransform = GetComponent<RectTransform>(); 
    foreach (Transform child in Canvas.transform)
      Destroy(child.gameObject);
    for (int i = 0; i < choices.Length; i++) {
      var angle = 2f*Mathf.PI * i / choices.Length;
      var card = Instantiate(CardPrefab, Canvas.transform);
      card.Init($"{choices[i]}");
      var rect = card.GetComponent<RectTransform>();
      rect.anchoredPosition = new(Radius*Mathf.Sin(angle), Radius*Mathf.Cos(angle));
    }
    Canvas.SetActive(true);
    IsShowing = true;
  }

  public void Select(int choice) {
    if (choice == -1) {
      EventSystem.current.SetSelectedGameObject(null);
    } else {
      var items = GetComponentsInChildren<BuildMenuItemUI>();
      EventSystem.current.SetSelectedGameObject(items[choice].gameObject);
    }
  }

  public void Hide() {
    Canvas.SetActive(false);
    IsShowing = false;
  }

  public void OnExit() {
    Hide();
  }
}
