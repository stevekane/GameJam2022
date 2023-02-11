using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace PlayblesGraphVisualization {
  public class PlayablesNode : Node {
    public List<Port> Inputs = new();
    public List<Port> Outputs = new();
  }
}