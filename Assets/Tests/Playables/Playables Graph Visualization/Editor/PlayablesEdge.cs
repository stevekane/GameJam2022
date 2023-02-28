using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace PlayablesGraphVisualization {
  public class PlayablesEdge : Edge {
    public PlayablesNode Source;
    public int SourceOutputPort;
    public PlayablesNode Destination;
    public int DestinationInputPort;

    public PlayablesEdge(
    PlayablesNode source,
    int sourceOutputPort,
    PlayablesNode destination,
    int destinationInputPort) {
      Source = source;
      SourceOutputPort = sourceOutputPort;
      Destination = destination;
      DestinationInputPort = destinationInputPort;
      input = Destination.Inputs[destinationInputPort];
      output = Source.Outputs[sourceOutputPort];
    }
  }
}