using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Timeline;
using UnityEditor.Experimental.GraphView;

public struct ConnectedOutput {
  static ConnectedOutput NullGraph = new();
  public static ConnectedOutput Null => NullGraph;
  public readonly Playable Destination;
  public readonly int PortIndex;
  public bool IsNull() => Equals(NullGraph);
  public ConnectedOutput(Playable destination, int portIndex) {
    Destination = destination;
    PortIndex = portIndex;
  }
}

public static class PlayableGraphExtensions {
  public static List<Playable> OriginalOutputs = new();
  public static List<Playable> AffectedOutputs = new();
  public static ConnectedOutput Connection(this Playable playable, int portIndex) {
    if (playable.IsNull())
      return ConnectedOutput.Null;
    var destinationInputPortCount = playable.GetInputCount();
    var maxDestinationInputPort = destinationInputPortCount-1;
    if (portIndex > maxDestinationInputPort)
      return ConnectedOutput.Null;
    var source = playable.GetInput(portIndex);
    if (source.IsNull())
      return ConnectedOutput.Null;
    var sourceOutputCount = source.GetOutputCount();
    if (sourceOutputCount <= 1)
      return new ConnectedOutput(source, 0);

    OriginalOutputs.Clear();
    AffectedOutputs.Clear();
    for (var o = 0; o < sourceOutputCount; o++) {
      OriginalOutputs.Add(source.GetOutput(o));
    }
    // TODO: What to do when playable.CanChangeInputs is false?
    // playable.DisconnectInput(portIndex);
    for (var o = 0; o < sourceOutputCount; o++) {
      AffectedOutputs.Add(source.GetOutput(o));
    }
    var sourceOutputPort = 0;
    for (var o = 0; o < sourceOutputCount; o++) {
      if (OriginalOutputs[o].GetHashCode() != AffectedOutputs[o].GetHashCode()) {
        sourceOutputPort = o;
      }
    }
    // playable.ConnectInput(portIndex, source, sourceOutputPort);
    return new ConnectedOutput(source, sourceOutputPort);
  }
}

namespace PlayablesGraphVisualization {
  public class PlayablesGraphView : GraphView {
    public PlayablesGraphView() {
      this.AddManipulator(new ContentDragger());
      this.AddManipulator(new ContentZoomer());
      this.AddManipulator(new ClickSelector());
      this.AddManipulator(new RectangleSelector());
      this.StretchToParentSize();
    }

    public void Render(PlayableGraph graph) {
      var outputNodeMap = new Dictionary<PlayableOutput, PlayablesNode>();
      var playableNodeMap = new Dictionary<Playable, PlayablesNode>();
      var edges = new List<PlayablesEdge>();

      void Connect(Playable parent, PlayablesNode parentNode, int inputIndex, int depth, int height) {
        var playable = parent.GetInput(inputIndex);
        if (!playable.IsNull()) {
          var connection = parent.Connection(inputIndex);
          var node = playableNodeMap.GetOrAdd(playable, delegate {
            return Create(depth, height, playable);
          });
          if (!connection.IsNull()) {
            var edge = new PlayablesEdge(node, connection.PortIndex, parentNode, inputIndex);
            edges.Add(edge);
            var inputCount = playable.GetInputCount();
            for (var i = 0; i < inputCount; i++) {
              Connect(playable, node, i, depth + 1, height + i);
            }
          }
        }
      }

      var outputCount = graph.GetOutputCount();
      for (var o = 0; o < outputCount; o++) {
        var output = graph.GetOutput(o);
        var outputNode = Create(0, o, output);
        outputNodeMap.Add(output, outputNode);
        var source = output.GetSourcePlayable();
        if (!source.IsNull()) {
          var node = playableNodeMap.GetOrAdd(source, delegate {
            return Create(1, o, source);
          });
          var edge = new PlayablesEdge(node, 0, outputNode, 0);
          edges.Add(edge);
          var inputCount = source.GetInputCount();
          for (var i = 0; i < inputCount; i++) {
            Connect(source, node, i, 2, o);
          }
        }
      }

      void Layout(PlayablesNode node) {
        const int WIDTH = 60;
        const int HEIGHT = 40;
        const int X_GAP = 250;
        const int Y_GAP = 250;
        node.SetPosition(new Rect(node.Depth * -X_GAP, node.Height * Y_GAP, WIDTH, HEIGHT));
      }

      outputNodeMap.Values.ForEach(Layout);
      playableNodeMap.Values.ForEach(Layout);
      outputNodeMap.Values.ForEach(AddElement);
      playableNodeMap.Values.ForEach(AddElement);
      edges.ForEach(AddElement);
    }

    PlayablesNode Create(int depth, int height, Playable playable) {
      var node = new PlayablesNode { title = PlayableName(playable) };
      var inputCount = playable.GetInputCount();
      for (var i = 0; i < inputCount; i++) {
        var orientation = Orientation.Horizontal;
        var direction = Direction.Input;
        var capacity = Port.Capacity.Single;
        var port = node.InstantiatePort(orientation, direction, capacity, null);
        node.Inputs.Add(port);
        node.inputContainer.Add(port);
      }
      var outputCount = playable.GetOutputCount();
      for (var i = 0; i < outputCount; i++) {
        var orientation = Orientation.Horizontal;
        var direction = Direction.Input;
        var capacity = Port.Capacity.Single;
        var port = node.InstantiatePort(orientation, direction, capacity, null);
        node.Outputs.Add(port);
        node.outputContainer.Add(port);
      }
      node.Depth = depth;
      node.Height = height;
      node.RefreshExpandedState();
      node.RefreshPorts();
      return node;
    }

    PlayablesNode Create(int depth, int height, PlayableOutput output) {
      var node = new PlayablesNode { title = OutputName(output) };
      var orientation = Orientation.Horizontal;
      var direction = Direction.Input;
      var capacity = Port.Capacity.Single;
      var port = node.InstantiatePort(orientation, direction, capacity, null);
      node.Inputs.Add(port);
      node.inputContainer.Add(port);
      node.Depth = depth;
      node.Height = height;
      node.RefreshExpandedState();
      node.RefreshPorts();
      return node;
    }

    string PlayableName(Playable playable) {
      var type = playable.GetPlayableType();
      return type switch {
        _ when type == typeof(AnimationClipPlayable) => "Animation",
        _ when type == typeof(AnimationMixerPlayable) => "Mixer",
        _ when type == typeof(AnimatorControllerPlayable) => "Animator",
        _ when type == typeof(AnimationLayerMixerPlayable) => "Layers",
        _ when type == typeof(AudioClipPlayable) => "Audio",
        _ when type == typeof(AudioMixer) => "Mixer",
        _ when type == typeof(TimelinePlayable) => "Timeline",
        _ when type.ToString().Contains("AnimationPose") => "Pose",
        _ when type.ToString().Contains("AnimationOffset") => "Offset",
        _ when type.ToString().Contains("AnimationMotionXToDelta") => "MotionXToDelta",
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