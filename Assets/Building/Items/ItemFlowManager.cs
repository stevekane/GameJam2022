using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ItemFlowManager : MonoBehaviour {
  public static ItemFlowManager Instance;

  class ItemFlow {
    public ItemObject Object;
    public Vector3 NextWorldPos;
    public Edge Edge;
    public List<Vector2Int> Path;

    public Vector2Int NextCell => BuildGrid.WorldToGrid(NextWorldPos);
  }

  List<ItemFlow> Items = new();
  Dictionary<Machine, int> PlayerCraftRequests = new();
  Dictionary<Edge, int> EdgeDemands = new();

  public void AddCraftRequest(Machine producer) {
    PlayerCraftRequests[producer] = PlayerCraftRequests.GetValueOrDefault(producer) + 1;
    BuildGraph(); // TODO: cache this
    SolveForRequests();
  }

  struct Edge {
    public Machine From;
    public int OutputIdx;
    public Machine To;
    public int InputIdx;

    public bool IsNull() => From == null;

    public override int GetHashCode() => (From, To, OutputIdx, InputIdx).GetHashCode();
    public override bool Equals(object other) => other is Edge e && (From, To, OutputIdx, InputIdx).Equals((e.From, e.To, e.OutputIdx, e.InputIdx));
    public override string ToString() => (From, OutputIdx, To, InputIdx).ToString();
  }
  List<Edge> Edges;
  List<Machine> Machines;
  Dictionary<Edge, Vector2Int> EdgeOutputCells = new();

  void BuildGraph() {
    Edges = new();
    Machines = FindObjectsOfType<Machine>().ToList();
    foreach (var machine in Machines) {
      var connected = machine.FindConnectedMachines();
      for (int i = 0; i < machine.Recipe.Inputs.Length; i++) {
        var input = machine.Recipe.Inputs[i];
        foreach ((var provider, var outputCell) in connected) {
          int outputIdx = Array.FindIndex(provider.Recipe.Outputs, output => output.Item == input.Item);
          if (outputIdx != -1) {
            var edge = new Edge { From = provider, OutputIdx = outputIdx, To = machine, InputIdx = i };
            if (!Edges.Contains(edge)) {
              Edges.Add(edge);
              EdgeOutputCells[edge] = outputCell;
            }
          }
        }
      }
    }
  }

  // TODO: clean this up ffs
  void SolveForRequests() {
    var edgeAmounts = new Dictionary<Edge, Decision>();
    var inputEdges = new Dictionary<(Machine, int), List<Edge>>();
    var outputEdges = new Dictionary<(Machine, int), List<Edge>>();

    var solver = SolverContext.GetContext();
    solver.ClearModel();
    var model = solver.CreateModel();

    int didx = 0;
    string DecisionName(string name) => $"{name}{didx++}".Replace("(Clone)", "").Replace(' ', '_').Replace(',', '_').Replace('(', '_').Replace(')', '_');
    Decision AddDecision(Domain domain, string name) {
      var d = new Decision(domain, DecisionName(name));
      model.AddDecision(d);
      return d;
    }

    // Main goal: minimize time spent crafting.
    var t = AddDecision(Domain.Real, "t");
    model.AddGoal("Goal", GoalKind.Minimize, t);
    model.AddConstraint(null, t >= 0);

    // Adds constraint on the maximum that can consumed/produced in the given time.
    Decision AddMaxAmountConstraint(Machine machine, Recipe.ItemAmount amount) {
      var decidedAmount = AddDecision(Domain.IntegerNonnegative, $"max_{machine.name}_{amount.Item.name}");
      var rate = amount.Count / machine.Recipe.CraftTime;
      model.AddConstraint(null, decidedAmount <= rate * t);
      return decidedAmount;
    }
    // Adds constraints on the recipe ratios by ensuring time to consume/produce each input/output is the same.
    void AddRecipeRatioConstraints(Machine machine, Decision[] inputAmounts, Decision[] outputAmounts) {
      var recipe = machine.Recipe;
      Term TimeToCraft(int idx, Decision[] decidedAmounts, Recipe.ItemAmount[] amounts) =>
        decidedAmounts[idx] / (amounts[idx].Count / recipe.CraftTime);
      var referenceTime =
        inputAmounts.Length > 0 ? TimeToCraft(0, inputAmounts, recipe.Inputs) :
        outputAmounts.Length > 0 ? TimeToCraft(0, outputAmounts, recipe.Outputs) : null;
      for (var i = 0; i < inputAmounts.Length; i++)
        model.AddConstraint(null, TimeToCraft(i, inputAmounts, recipe.Inputs) == referenceTime);
      for (var i = 0; i < outputAmounts.Length; i++)
        model.AddConstraint(null, TimeToCraft(i, outputAmounts, recipe.Outputs) == referenceTime);
    }
    var empty = new List<Edge>();
    // Adds constraint: sum(edges attached to machine slot N) = machine's slot_N decision.
    void AddEdgeToMachineConstraints(Machine machine, Dictionary<(Machine, int), List<Edge>> edgeMap, Recipe.ItemAmount[] slots, Decision[] decidedAmounts) {
      for (var i = 0; i < slots.Length; i++) {
        var edges = edgeMap.GetValueOrDefault((machine, i), empty);
        if (edges.Count == 0) continue; // TODO: figure out what to do about a disconnected input, and output
        var edgeSum = edges.Aggregate((Term)0, (term, e) => term + edgeAmounts[e]);
        model.AddConstraint(null, edgeSum == decidedAmounts[i]);
      }
    }

    // Add decision variables for each edge.
    foreach (var edge in Edges) {
      edgeAmounts[edge] = AddDecision(Domain.IntegerNonnegative, $"e_{edge}");
      outputEdges.GetOrAdd((edge.From, edge.OutputIdx), () => new()).Add(edge);
      inputEdges.GetOrAdd((edge.To, edge.InputIdx), () => new()).Add(edge);
    }

    // Set up the constraints for each machine's input and output, and collect the decided output amounts.
    foreach (var machine in Machines) {
      var inputAmounts = machine.Recipe.Inputs.Select(amount => AddMaxAmountConstraint(machine, amount)).ToArray();
      var outputAmounts = machine.Recipe.Outputs.Select(amount => AddMaxAmountConstraint(machine, amount)).ToArray();
      AddRecipeRatioConstraints(machine, inputAmounts, outputAmounts);
      AddEdgeToMachineConstraints(machine, inputEdges, machine.Recipe.Inputs, inputAmounts);
      AddEdgeToMachineConstraints(machine, outputEdges, machine.Recipe.Outputs, outputAmounts);

      if (PlayerCraftRequests.TryGetValue(machine, out var numCrafts)) {
        // Only need to constrain one output to ensure the machine will craft it.
        if (machine.Recipe.Outputs.Length > 0) {
          var outputsRequested = machine.Recipe.Outputs[0].Count * numCrafts;
          model.AddConstraint(null, outputAmounts[0] >= outputsRequested);
        } else {
          var inputsRequested = machine.Recipe.Inputs[0].Count * numCrafts;
          model.AddConstraint(null, inputAmounts[0] >= inputsRequested);
        }
      }
    }

    var solution = solver.Solve(new Directive() { TimeLimit = 5000 });

    //foreach (var c in model.Constraints) {
    //  Debug.Log($"constraint: {c.Expression}");
    //}
    //Debug.Log($"Result time={t}");

    foreach (var edge in Edges) {
      var amount = (int)edgeAmounts[edge].ToDouble();
      EdgeDemands[edge] = amount;
      if (amount > 0)
        edge.From.TryStartCrafting();
      Debug.Log($"Edge {edge} has demand {edgeAmounts[edge]}");
    }
  }

  public void OnCraftFinished(Machine machine, int[] outputQueue) {
    if (PlayerCraftRequests.TryGetValue(machine, out var numCrafts) && numCrafts > 0)
      PlayerCraftRequests[machine] = numCrafts - 1;
    for (var i = 0; i < outputQueue.Length; i++) {
      var edgeDemand = EdgeDemands.FirstOrDefault(e => e.Key.From == machine && e.Key.OutputIdx == i);
      if (!edgeDemand.Key.IsNull())
        TryOutput(edgeDemand.Key);
    }
  }

  void TryOutput(Edge edge) {
    Debug.Assert(EdgeDemands[edge] > 0);

    var outputCell = EdgeOutputCells[edge];
    var item = edge.From.Recipe.Outputs[edge.OutputIdx].Item;
    if (IsCellOccupied(outputCell, item))
      return;
    if (!edge.From.TryOutputItem(edge.OutputIdx))
      return;

    var instance = item.Spawn(edge.From.transform.position);
    var flow = new ItemFlow {
      Object = instance,
      Path = ComputePath(outputCell, edge),
      Edge = edge,
    };
    AdvanceOnPath(flow);
    Items.Add(flow);

    if (--EdgeDemands[edge] > 0) {
      edge.From.TryStartCrafting();
    } else {
      EdgeDemands.Remove(edge);
    }
  }

  List<Vector2Int> ComputePath(Vector2Int startCell, Edge edge) {
    var destCell = startCell;
    var y = edge.From.transform.position.y;
    var toVisit = new Queue<Vector2Int>();
    var distance = new Dictionary<Vector2Int, int>();
    var prev = new Dictionary<Vector2Int, Vector2Int>();
    toVisit.Enqueue(startCell);
    distance[startCell] = 0;
    while (toVisit.TryDequeue(out var pos)) {
      var obj = BuildGrid.GetCellContents(pos, y);
      if (obj == null) continue;
      if (obj == gameObject || obj.TryGetComponent(out Capillary _)) {
        void Check(Vector2Int neighbor) {
          var alt = distance[pos] + 1;
          if (alt < distance.GetValueOrDefault(neighbor, int.MaxValue)) {
            distance[neighbor] = distance[pos] + 1;
            prev[neighbor] = pos;
            toVisit.Enqueue(neighbor);
          }
        }
        Check(pos + Vector2Int.left);
        Check(pos + Vector2Int.right);
        Check(pos + Vector2Int.up);
        Check(pos + Vector2Int.down);
      }
      if (obj != gameObject && obj.TryGetComponent(out Machine machine) && machine == edge.To)
        destCell = pos;
    }
    List<Vector2Int> path = new();
    for (var pos = destCell; pos != startCell; pos = prev[pos])
      path.Add(pos);
    path.Add(startCell);
    path.Reverse();
    return path;
  }

  public bool IsCellOccupied(Vector2Int pos, ItemInfo itemInfo) {
    return Items.Any(item => item.Object == itemInfo && item.NextCell == pos);
  }

  void MoveItems() {
    const float speed = 4f;
    var distance = speed * Time.fixedDeltaTime;
    List<ItemFlow> toConsume = new();
    foreach (var item in Items) {
      var delta = item.NextWorldPos - item.Object.transform.position;
      if (delta.sqrMagnitude < distance.Sqr()) {
        if (item.Path.Count == 0) {
          toConsume.Add(item);
        } else {
          AdvanceOnPath(item);
        }
      } else {
        item.Object.transform.position += delta.normalized * distance;
      }
    }
    foreach (var item in toConsume) {
      item.Edge.To.InsertItem(item.Edge.InputIdx);
      item.Object.gameObject.Destroy();
    }
    toConsume.ForEach(f => Items.Remove(f));
  }

  void AdvanceOnPath(ItemFlow item) {
    if (IsCellOccupied(item.Path[0], item.Object.Info)) return;
    item.NextWorldPos = BuildGrid.GridToWorld(item.Path[0], item.Edge.From.transform.position.y);
    item.Path.RemoveAt(0);
  }

  void FixedUpdate() {
    MoveItems();
  }

#if true
  Machine NewMachine(Recipe recipe, string name) {
    var go = new GameObject(name);
    go.SetActive(false);
    go.AddComponent<Animator>();
    go.AddComponent<BuildObject>();
    var m = go.AddComponent<Machine>();
    m.Recipe = recipe;
    go.SetActive(true);
    return m;
  }
  Recipe NewRecipe(float craftTime, Recipe.ItemAmount[] inputs, Recipe.ItemAmount output) {
    var recipe = ScriptableObject.CreateInstance<Recipe>();
    recipe.Inputs = inputs;
    recipe.Outputs = new[] { output };
    recipe.CraftTime = craftTime;
    return recipe;
  }
  Recipe.ItemAmount NewIngred(ItemInfo info, int count) {
    var amount = new Recipe.ItemAmount {
      Item = info,
      Count = count
    };
    return amount;
  }
  [ContextMenu("Test flow")]
  void Test() {
    var itemIron = ScriptableObject.CreateInstance<ItemInfo>();
    var itemGear = ScriptableObject.CreateInstance<ItemInfo>();
    var itemBelt = ScriptableObject.CreateInstance<ItemInfo>();
    var recipeMiner = NewRecipe(1f, new Recipe.ItemAmount[0], NewIngred(itemIron, 1));
    var recipeGear = NewRecipe(1f, new[] { NewIngred(itemIron, 2) }, NewIngred(itemGear, 1));
    var recipeBelt = NewRecipe(1f, new[] { NewIngred(itemGear, 1), NewIngred(itemIron, 1) }, NewIngred(itemBelt, 1));
    Machines = new() {
      NewMachine(recipeMiner, "ma"),
      NewMachine(recipeMiner, "mb"),
      NewMachine(recipeGear, "g"),
      NewMachine(recipeBelt, "belt"),
    };
    Edges = new() {
      new() { From = Machines[0], OutputIdx = 0, To = Machines[2], InputIdx = 0 },
      //new() { From = Machines[1], OutputIdx = 0, To = Machines[2], InputIdx = 0 },
      new() { From = Machines[0], OutputIdx = 0, To = Machines[3], InputIdx = 1 },
      //new() { From = Machines[1], OutputIdx = 0, To = Machines[3], InputIdx = 1 },
      new() { From = Machines[2], OutputIdx = 0, To = Machines[3], InputIdx = 0 },
    };
    PlayerCraftRequests[Machines[3]] = 1;

    SolveForRequests();

    Edges = new();
    Machines.ForEach(m => DestroyImmediate(m.gameObject));
  }
#endif

}
