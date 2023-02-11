using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEditor.Experimental.GraphView;

public struct PlayableGraphConnection {
  static PlayableGraphConnection NullGraph = new();
  public Playable Source;
  public Playable Destination;
  public int SourceOutputPort;
  public int DestinationInputPort;
  public static PlayableGraphConnection Null => NullGraph;
  public bool IsNull() => Equals(NullGraph);
}

public static class PlayableGraphExtensions {
  public static List<Playable> OriginalOutputs = new();
  public static List<Playable> AffectedOutputs = new();
  public static PlayableGraphConnection Connection(
  this PlayableGraph graph,
  Playable destination,
  int destinationInputPort) {
    if (destination.IsNull())
      return PlayableGraphConnection.Null;
    var destinationInputPortCount = destination.GetInputCount();
    var maxDestinationInputPort = destinationInputPortCount-1;
    if (destinationInputPort > maxDestinationInputPort)
      return PlayableGraphConnection.Null;
    var source = destination.GetInput(destinationInputPort);
    if (source.IsNull())
      return PlayableGraphConnection.Null;
    OriginalOutputs.Clear();
    AffectedOutputs.Clear();
    var sourceOutputCount = source.GetOutputCount();
    for (var o = 0; o < sourceOutputCount; o++) {
      OriginalOutputs.Add(source.GetOutput(o));
    }
    destination.DisconnectInput(destinationInputPort);
    for (var o = 0; o < sourceOutputCount; o++) {
      AffectedOutputs.Add(source.GetOutput(o));
    }
    var sourceOutputPort = 0;
    for (var o = 0; o < sourceOutputCount; o++) {
      if (OriginalOutputs[o].GetHashCode() != AffectedOutputs[o].GetHashCode()) {
        sourceOutputPort = o;
      }
    }
    destination.ConnectInput(destinationInputPort, source, sourceOutputPort);
    return new PlayableGraphConnection {
      Source = source,
      Destination = destination,
      SourceOutputPort = sourceOutputPort,
      DestinationInputPort = destinationInputPort
    };
  }
}

namespace PlayblesGraphVisualization {
  public class PlayablesGraphView : GraphView {
    const int WIDTH = 60;
    const int HEIGHT = 40;
    const int X_GAP = 250;
    const int Y_GAP = 150;

    PlayableGraph Graph;

    public PlayablesGraphView() {
      this.AddManipulator(new ContentDragger());
      this.AddManipulator(new ContentZoomer());
      this.AddManipulator(new ClickSelector());
      this.AddManipulator(new RectangleSelector());
      this.StretchToParentSize();
    }

    public void Setup() {
      Graph = PlayableGraph.Create("Visualization Test Graph");
      var animClipPlayable1 = AnimationClipPlayable.Create(Graph, null);
      var animClipPlayable2 = AnimationClipPlayable.Create(Graph, null);
      var crossedPassthrough = ScriptPlayable<NoopBehavior>.Create(Graph);
      var animationMixer = AnimationMixerPlayable.Create(Graph);
      var noopPlayable = ScriptPlayable<NoopBehavior>.Create(Graph);
      var orphanedAnimationClipPlayable = AnimationClipPlayable.Create(Graph, null);
      var animationOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", null);
      crossedPassthrough.AddInput(animClipPlayable1, 0, 1);
      crossedPassthrough.AddInput(animClipPlayable2, 0, 1);
      crossedPassthrough.SetOutputCount(2);
      animationMixer.AddInput(crossedPassthrough, 1, 1);
      animationMixer.AddInput(crossedPassthrough, 0, 1);
      noopPlayable.AddInput(animationMixer, 0, 1);
      animationOutput.SetSourcePlayable(noopPlayable, 0);

      var outputNodeMap = new Dictionary<PlayableOutput, PlayablesNode>();
      var playableNodeMap = new Dictionary<Playable, PlayablesNode>();
      var edges = new List<Edge>();

      void Connect(Playable parent, PlayablesNode parentNode, int inputIndex, int depth, int height) {
        var playable = parent.GetInput(inputIndex);
        if (!playable.IsNull()) {
          var connection = parent.GetGraph().Connection(parent, inputIndex);
          var node = playableNodeMap.GetOrAdd(playable, delegate {
            return Create(-X_GAP * depth, Y_GAP * height, playable);
          });
          if (!connection.IsNull()) {
            var edge = new Edge {
              input = parentNode.Inputs[connection.DestinationInputPort],
              output = node.Outputs[connection.SourceOutputPort]
            };
            edges.Add(edge);
            var inputCount = playable.GetInputCount();
            for (var i = 0; i < inputCount; i++) {
              Connect(playable, node, i, depth + 1, height + i);
            }
          }
        }
      }

      var outputCount = Graph.GetOutputCount();
      for (var o = 0; o < outputCount; o++) {
        var output = Graph.GetOutput(o);
        var outputNode = Create(0, Y_GAP * o, output);
        outputNodeMap.Add(output, outputNode);
        var source = output.GetSourcePlayable();
        if (!source.IsNull()) {
          var node = playableNodeMap.GetOrAdd(source, delegate {
            return Create(-X_GAP, Y_GAP * o, source);
          });
          var edge = new Edge { output = node.Outputs[0], input = outputNode.Inputs[0] };
          edges.Add(edge);
          var inputCount = source.GetInputCount();
          for (var i = 0; i < inputCount; i++) {
            Connect(source, node, i, 2, o);
          }
        }
      }

      outputNodeMap.Values.ForEach(AddElement);
      playableNodeMap.Values.ForEach(AddElement);
      edges.ForEach(AddElement);
    }

    public void Teardown() {
      if (Graph.IsValid()) {
        Graph.Destroy();
      }
    }

    PlayablesNode Create(int x, int y, Playable playable) {
      var node = new PlayablesNode { title = PlayableName(playable) };
      var inputCount = playable.GetInputCount();
      for (var i = 0; i < inputCount; i++) {
        var orientation = Orientation.Horizontal;
        var direction = Direction.Input;
        var capacity = Port.Capacity.Single;
        var port = node.InstantiatePort(orientation, direction, capacity, typeof(int));
        port.portName = i.ToString();
        node.Inputs.Add(port);
        node.inputContainer.Add(port);
      }
      var outputCount = playable.GetOutputCount();
      for (var i = 0; i < outputCount; i++) {
        var orientation = Orientation.Horizontal;
        var direction = Direction.Input;
        var capacity = Port.Capacity.Single;
        var port = node.InstantiatePort(orientation, direction, capacity, typeof(int));
        port.portName = i.ToString();
        node.Outputs.Add(port);
        node.outputContainer.Add(port);
      }
      node.RefreshExpandedState();
      node.RefreshPorts();
      node.SetPosition(new Rect(x, y, WIDTH, HEIGHT));
      return node;
    }

    PlayablesNode Create(int x, int y, PlayableOutput output) {
      var node = new PlayablesNode { title = OutputName(output) };
      var orientation = Orientation.Horizontal;
      var direction = Direction.Input;
      var capacity = Port.Capacity.Single;
      var port = node.InstantiatePort(orientation, direction, capacity, typeof(int));
      port.portName = "source";
      node.Inputs.Add(port);
      node.inputContainer.Add(port);
      node.RefreshExpandedState();
      node.RefreshPorts();
      node.SetPosition(new Rect(x, y, WIDTH, HEIGHT));
      return node;
    }

    string PlayableName(Playable playable) {
      var type = playable.GetPlayableType();
      return type switch {
        _ when type == typeof(AnimationClipPlayable) => "Animation Clip",
        _ when type == typeof(AnimationMixerPlayable) => "Animation Mixer",
        _ when type == typeof(AnimationLayerMixerPlayable) => "Animation Layer Mixer",
        _ => type.ToString()
      };
    }

    string OutputName(PlayableOutput output) {
      var type = output.GetPlayableOutputType();
      return type switch {
        _ when type == typeof(AnimationPlayableOutput) => "Animation Output",
        _ when type == typeof(AudioPlayableOutput) => "Audio Output",
        _ => type.ToString()
      };
    }
  }
}