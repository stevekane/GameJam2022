using System.Collections.Generic;
using UnityEngine;

namespace ActionsAndAI {
  public class ActionManager : MonoBehaviour {
    public List<IAction> Actions = new();
  }
}