using UnityEngine;

public class UI : MonoBehaviour {
  [SerializeField] int MaxInstances = 32;
  [SerializeField] float VerticalOffset = .5f;
  [SerializeField] Selector SelectorPrefab;
  [SerializeField] Selector HighlighterPrefab;
  [SerializeField] AimMeter AimMeter;
  Selector Selector;
  Selector[] Highlighters;

  void Start() {
    Selector = Instantiate(SelectorPrefab,transform);
    Highlighters = new Selector[MaxInstances];
    for (var i = 0; i < MaxInstances; i++) {
      Highlighters[i] = Instantiate(HighlighterPrefab,transform);
    }
  }

  public void Select(Targetable target) {
    if (target) {
      Selector.gameObject.SetActive(true);
      Selector.Target = target;
    } else {
      Selector.Target = null;
      Selector.gameObject.SetActive(false);
    }
  }

  public void Highlight(Targetable[] targets, int count) {
    int i = 0;
    for (int n = 0; n < count; n++) {
      var highlighter = Highlighters[n];
      var target = targets[n];
      var position = target.transform.position;
      position.y = position.y + target.Height + VerticalOffset;
      highlighter.gameObject.SetActive(true);
      highlighter.transform.SetParent(target.transform,false);
      highlighter.transform.position = position;
      i++;
    }
    for (int n = i; n < Highlighters.Length; n++) {
      var highlighter = Highlighters[n];
      highlighter.gameObject.SetActive(false);
      highlighter.transform.SetParent(transform,false);
    }
  }

  public void SetAimMeter(bool display, Vector3 position, int value, int maxValue) {
    if (display) {
      AimMeter.gameObject.SetActive(true);
      AimMeter.SetFill(maxValue,value);
      AimMeter.transform.position = position;
      AimMeter.transform.LookAt(position + Camera.main.transform.forward);
    } else {
      AimMeter.gameObject.SetActive(false);
    }
  }
}