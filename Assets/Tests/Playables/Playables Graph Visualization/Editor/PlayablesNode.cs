using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace PlayablesGraphVisualization {
  public class PlayablesNode : Node {
    public List<Port> Inputs = new();
    public List<Port> Outputs = new();
    public int Depth;
    public int Height;
  }
}