using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(ButtonEventActionMap))]
public class ButtonEventActionMapPropertyDrawer : PropertyDrawer {
  public override VisualElement CreatePropertyGUI(SerializedProperty property) {
    var container = new VisualElement();
    var code = new PropertyField(property.FindPropertyRelative("ButtonCode"), "");
    var type = new PropertyField(property.FindPropertyRelative("ButtonPressType"), "");
    var reference = new PropertyField(property.FindPropertyRelative("ActionReference"), "");
    var flow = new PropertyField(property.FindPropertyRelative("FlowBehavior"), "");

    flow.RegisterValueChangeCallback(v => {
      flow.style.backgroundColor = (EventFlowBehavior)v.changedProperty.enumValueIndex switch {
        EventFlowBehavior.Block => Color.red,
        EventFlowBehavior.Pass => Color.white,
        _ => Color.green
      };
    });

    code.style.flexBasis = 0;
    code.style.flexGrow = 1;
    code.style.flexShrink = 1;

    type.style.flexBasis = 0;
    type.style.flexGrow = 1;
    type.style.flexShrink = 1;

    reference.style.flexBasis = 0;
    reference.style.flexGrow = 2;
    reference.style.flexShrink = 2;

    flow.style.flexBasis = 0;
    flow.style.flexGrow = 1;
    flow.style.flexShrink = 1;

    container.Add(code);
    container.Add(type);
    container.Add(reference);
    container.Add(flow);

    container.style.flexDirection = FlexDirection.Row;
    container.style.justifyContent = Justify.Center;
    container.style.alignItems = Align.Center;
    return container;
  }
}