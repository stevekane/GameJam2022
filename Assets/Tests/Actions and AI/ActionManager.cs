using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ActionsAndAI {
  public class ActionManager : MonoBehaviour {
    public List<IAction> Actions = new();
    public List<IAxisAction> AxisActions = new();
    public TaskScope Scope = new();

    #if UNITY_EDITOR
    void FixedUpdate() {
      var log = "";
      Actions
        .Where(a => a.CanStart())
        .ForEach(a => log += $"{a.Name}  |  {a.ButtonCode}+{a.ButtonPressType}\n");
      AxisActions
        .Where(a => a.CanStart())
        .ForEach(a => log += $"{a.Name}  |  {a.AxisCode}\n");
      DebugUI.Log(this, log);
    }
    #endif
  }
}