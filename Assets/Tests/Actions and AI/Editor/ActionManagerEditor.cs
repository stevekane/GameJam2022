using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace ActionsAndAI {
  [CustomEditor(typeof(ActionManager))]
  public class ActionManagerEditor : Editor {
    public override VisualElement CreateInspectorGUI() {
      var actionManager = (ActionManager)target;
      var container = new VisualElement();
      foreach (var action in actionManager.Actions) {
        var text = new TextElement();
        text.text = $"{action.name} ({action.CanStart()})";
        container.Add(text);
      }
      foreach (var action in actionManager.AxisActions) {
        var text = new TextElement();
        text.text = $"{action.name} ({action.CanStart()})";
        container.Add(text);
      }
      return container;
    }
  }
}