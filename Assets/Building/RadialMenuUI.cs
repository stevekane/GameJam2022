using UnityEngine;
using UnityEngine.EventSystems;

public class RadialMenuUI : MonoBehaviour {
  public GameObject Canvas;
  public RadialMenuUIItem CardPrefab;
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
      var items = GetComponentsInChildren<RadialMenuUIItem>();
      EventSystem.current.SetSelectedGameObject(items[choice].gameObject);
    }
  }

  public int GetSelectedFromAim(Vector3 dir, int numChoices) {
    if (dir == Vector3.zero)
      return -1;
    var angle = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
    if (numChoices > 1)
      angle += 90f / (numChoices-1);  // Offset the start region for the choices by the width of the region
    var frac = (1f + angle/360f) % 1f;
    var idx = (int)(frac * numChoices);
    return idx;
  }


  public void Hide() {
    Canvas.SetActive(false);
    IsShowing = false;
  }

  public void OnExit() {
    Hide();
  }
}
