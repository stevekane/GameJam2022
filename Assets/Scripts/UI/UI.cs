using UnityEngine;

public class UI : MonoBehaviour {
  [SerializeField] int MaxInstances = 128;
  [SerializeField] float VerticalOffset = .5f;
  [SerializeField] Selector SelectorPrefab;
  [SerializeField] Selector HighlighterPrefab;
  [SerializeField] AimMeter AimMeter;
  Selector Selector;
  Selector[] Highlighters;

  void Awake() {
    Selector = Instantiate(SelectorPrefab,transform);
    Highlighters = new Selector[MaxInstances];
    for (var i = 0; i < MaxInstances; i++) {
      Highlighters[i] = Instantiate(HighlighterPrefab,transform);
    }
  }

  void LateUpdate() {
    if (AimMeter.Target) {
      var position = AimMeter.Target.transform.position;
      position.y = position.y + AimMeter.Height;
      AimMeter.transform.position = position;
      AimMeter.transform.LookAt(position + Camera.main.transform.forward);
    }
    if (Selector.Target) {
      var position = Selector.Target.transform.position;
      position.y = position.y + Selector.Target.Height + VerticalOffset;
      Selector.transform.position = position;
    }
    for (int i = 0; i < Highlighters.Length; i++) {
      var highlighter = Highlighters[i];
      var target = highlighter.Target;
      if (target) {
        var position = target.transform.position;
        position.y = position.y + target.Height + VerticalOffset;
        Highlighters[i].transform.position = position;
      }
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
      highlighter.Target = target;
      i++;
    }
    for (int n = i; n < Highlighters.Length; n++) {
      var highlighter = Highlighters[n];
      highlighter.gameObject.SetActive(false);
      highlighter.Target = null;
    }
  }

  public void SetAimMeter(Transform target, bool display, int value, int maxValue) {
    if (display) {
      AimMeter.gameObject.SetActive(true);
      AimMeter.Target = target;
      AimMeter.SetFill(maxValue,value);
    } else {
      AimMeter.gameObject.SetActive(false);
      AimMeter.Target = null;
    }
  }
}