using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour {
  public static DebugUI Instance;
  [SerializeField] TextMeshProUGUI TextUI;

  public static void Log(Object obj, string msg) {
    DebugUI.Instance?.LogInternal(obj, msg);
  }

  void LogInternal(Object obj, string msg) {
    Dirty = true;
    Logs[obj.GetHashCode()] = msg + "\n";
  }

  bool Dirty = false;
  Dictionary<int, string> Logs = new();
  void FixedUpdate() {
    if (!Dirty) return;
    Dirty = false;

    string text = "";
    foreach (var (id, msg) in Logs)
      text += msg;
    TextUI.text = text;
  }
}