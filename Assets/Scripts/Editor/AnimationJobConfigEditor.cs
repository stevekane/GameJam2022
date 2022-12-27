using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimationJobConfig))]
public class AnimationJobConfigDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.PropertyField(position, property, label, true);
    if (property.isExpanded) {
      if (GUI.Button(new Rect(position.xMin + 30f, position.yMax - 20f, position.width - 60f, 20f), "Reset Phases")) {
        var config = (AnimationJobConfig)property.boxedValue;
        var phases = config.Clip.events.Select(e => Timeval.FromSeconds(e.time)).ToList();
        phases.Add(Timeval.FromSeconds(config.Clip.length));
        for (var i = phases.Count-1; i > 0; i--)  // Convert from absolute time to duration
          phases[i] = Timeval.FromSeconds(phases[i].Seconds - phases[i-1].Seconds);
        config.PhaseDurations = phases.ToArray();
        property.boxedValue = config;
      }
    }
  }

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return EditorGUI.GetPropertyHeight(property) + (property.isExpanded ? 20f : 0f);
  }
}