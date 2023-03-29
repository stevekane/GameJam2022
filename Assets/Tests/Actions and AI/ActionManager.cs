using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ActionsAndAI {
  public class ActionManager : MonoBehaviour {
    public List<AbstractActionBehavior> Actions = new();
    public List<AbstractVectorActionBehavior> AxisActions = new();
    public TaskScope Scope = new();

    #if UNITY_EDITOR
    void FixedUpdate() {
      var log = "";
      Actions
        .Where(a => a.CanStart())
        .ForEach(a => log += $"{a.name}\n");
      AxisActions
        .Where(a => a.CanStart())
        .ForEach(a => log += $"{a.name}\n");
      DebugUI.Log(this, log);
    }
    #endif
  }
}