using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEditor.Experimental.GraphView;

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

    /*
    A BIG NOTE ABOUT AN IMPORTANT FUCKUP / CONCESSION.

    It does not appear possible to query for connections in Graph that include the
    input and output ports. I know, I know that sounds completely fucking idiotic
    since literally every single connection API requires both the input and output
    ports. And yet here we are...living in a community of broken people shattered by
    the consistent failure of Unity to create proper APIs. A true hell-scape.

    For now, I think what I have to settle for is the following disgusting things:

      Output ports are Capacity.Multiple
      All inputs connected to the zeroeth port on the connected node (which is absolute bullshit)

    This is heinously wrong and a gross oversight if it's truly the case.
    */
    public void Setup() {
      // Build a sample PlayableGraph
      Graph = PlayableGraph.Create("Visualization Test Graph");
      var animClipPlayable1 = AnimationClipPlayable.Create(Graph, null);
      var animClipPlayable2 = AnimationClipPlayable.Create(Graph, null);
      var animationMixer = AnimationMixerPlayable.Create(Graph);
      var noopPlayable = ScriptPlayable<NoopBehavior>.Create(Graph, null);
      var orphanedAnimationClipPlayable = AnimationClipPlayable.Create(Graph, null);
      var animationOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", null);
      animationMixer.AddInput(animClipPlayable1, 0, 1);
      animationMixer.AddInput(animClipPlayable2, 0, 1);
      noopPlayable.AddInput(animationMixer, 0, 1);
      animationOutput.SetSourcePlayable(noopPlayable, 0);

      // TODO: For a very large graph this might be too recursive?
      void Connect(Playable parent, PlayablesNode parentNode, int inputIndex, int depth, int height) {
        var playable = parent.GetInput(inputIndex);
        if (!playable.IsNull()) {
          var node = Create(-X_GAP * depth, Y_GAP * height, playable);
          var edge = new Edge { input = parentNode.Inputs[inputIndex], output = node.Outputs[0] };
          AddElement(node);
          AddElement(edge);
          var inputCount = playable.GetInputCount();
          for (var i = 0; i < inputCount; i++) {
            Connect(playable, node, i, depth + 1, height + i);
          }
        }
      }

      // recursively walk backwards from an output constructing all nodes not yet found
      var outputCount = Graph.GetOutputCount();
      for (var o = 0; o < outputCount; o++) {
        var output = Graph.GetOutput(o);
        var outputNode = Create(0, Y_GAP * o, output);
        AddElement(outputNode);
        var source = output.GetSourcePlayable();
        if (!source.IsNull()) {
          var node = Create(-X_GAP, Y_GAP * o, source);
          var edge = new Edge { output = node.Outputs[0], input = outputNode.Inputs[0] };
          AddElement(node);
          AddElement(edge);
          var inputCount = source.GetInputCount();
          for (var i = 0; i < inputCount; i++) {
            Connect(source, node, i, 2, o);
          }
        }
      }
    }

    public void Teardown() {
      if (Graph.IsValid()) {
        Graph.Destroy();
      }
    }

    PlayablesNode Create(int x, int y, Playable playable) {
      var node = new PlayablesNode { title = PlayableName(playable) };
      var inputCount = playable.GetInputCount();
      var outputCount = playable.GetOutputCount();
      for (var i = 0; i < inputCount; i++) {
        var orientation = Orientation.Horizontal;
        var direction = Direction.Input;
        var capacity = Port.Capacity.Single;
        var port = node.InstantiatePort(orientation, direction, capacity, typeof(int));
        port.portName = i.ToString();
        node.Inputs.Add(port);
        node.inputContainer.Add(port);
      }
      for (var i = 0; i < outputCount; i++) {
        var orientation = Orientation.Horizontal;
        var direction = Direction.Input;
        var capacity = Port.Capacity.Multi;
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