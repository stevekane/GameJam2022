using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
  Dictionary<Crafter, int> PlayerCraftRequests = new();
  Dictionary<Edge, int> EdgeDemands = new();

  public void AddCraftRequest(Crafter producer) {
    PlayerCraftRequests[producer] = PlayerCraftRequests.GetValueOrDefault(producer) + 1;
    BuildGraph(); // TODO: cache this
    SolveForRequests();
  }

  struct Edge {
    public Crafter From;
    public int OutputIdx;
    public Crafter To;
    public int InputIdx;

    public bool IsNull() => From == null;

    public override int GetHashCode() => (From, To, OutputIdx, InputIdx).GetHashCode();
    public override bool Equals(object other) => other is Edge e && (From, To, OutputIdx, InputIdx).Equals((e.From, e.To, e.OutputIdx, e.InputIdx));
    public override string ToString() => (From, OutputIdx, To, InputIdx).ToString();
  }
  List<Edge> Edges;
  List<Crafter> Crafters;
  Dictionary<Edge, Vector2Int> EdgeOutputCells = new();

  void BuildGraph() {
    Edges = new();
    Crafters = FindObjectsOfType<Crafter>().ToList();
    foreach (var crafter in Crafters) {
      var connected = crafter.FindConnectedCrafters();
      for (int i = 0; i < crafter.Recipe.Inputs.Length; i++) {
        var input = crafter.Recipe.Inputs[i];
        foreach ((var provider, var outputCell) in connected) {
          int outputIdx = Array.FindIndex(provider.Recipe.Outputs, output => output.Item == input.Item);
          if (outputIdx != -1) {
            var edge = new Edge { From = provider, OutputIdx = outputIdx, To = crafter, InputIdx = i };
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
    var edgeTimes = new Dictionary<Edge, Decision>();
    var inputEdges = new Dictionary<(Crafter, int), List<Edge>>();
    var outputEdges = new Dictionary<(Crafter, int), List<Edge>>();

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
    var t = AddDecision(Domain.RealNonnegative, "t");
    model.AddGoal("Goal", GoalKind.Minimize, t);

    var empty = new List<Edge>();
    // Adds constraint on the maximum that can consumed/produced in the given time.
    Decision AddMaxAmountDecision(Crafter crafter, Recipe.ItemAmount amount) {
      var decidedAmount = AddDecision(Domain.IntegerNonnegative, $"c_{crafter.name}_{amount.Item.name}");
      return decidedAmount;
    }
    // Adds constraints on the craft time of the crafter and the total craft time to deliver each output (edgeTimes[e]).
    void AddCraftTimeConstraints(Crafter crafter, Decision[] inputAmounts, Decision[] outputAmounts) {
      var recipe = crafter.Recipe;
      Term TimeToCraft(int idx, Decision[] decidedAmounts, Recipe.ItemAmount[] amounts) =>
        decidedAmounts[idx] / (amounts[idx].Count / recipe.CraftTime);
      var referenceTime =
        inputAmounts.Length > 0 ? TimeToCraft(0, inputAmounts, recipe.Inputs) :
        outputAmounts.Length > 0 ? TimeToCraft(0, outputAmounts, recipe.Outputs) : null;
      for (var i = 0; i < inputAmounts.Length; i++)
        model.AddConstraint(null, TimeToCraft(i, inputAmounts, recipe.Inputs) == referenceTime);
      for (var i = 0; i < outputAmounts.Length; i++)
        model.AddConstraint(null, TimeToCraft(i, outputAmounts, recipe.Outputs) == referenceTime);

      // Edge craft times = total time from system start to satisfy the given edge's full demand.
      // Note: this is not strictly correct. If a crafter has 2 output edges, it might satisfy the first edge at time T,
      // then the second at time T + T2. But this is a good enough approximation for our purposes - minimizing total
      // craft time.
      var totalCraftTime = AddDecision(Domain.RealNonnegative, $"ct_{crafter.name}");
      if (inputAmounts.Length == 0) {
        model.AddConstraint(null, totalCraftTime == referenceTime);
      } else {
        // Adds constraint: totalCraftTime = max(inputEdgeTime) + crafterCraftTime
        foreach ((var inputEdge, var inputEdgeTime) in edgeTimes) {
          if (inputEdge.To != crafter) continue;
          model.AddConstraint(null, totalCraftTime >= referenceTime + edgeTimes[inputEdge]);
        }
      }

      foreach ((var outputEdge, var outputEdgeTime) in edgeTimes) {
        if (outputEdge.From != crafter) continue;
        model.AddConstraint(null, outputEdgeTime == totalCraftTime);
      }
      model.AddConstraint(null, t >= totalCraftTime);
    }
    // Adds constraint: sum(edges attached to crafter slot N) = crafter's slot_N decision.
    void AddEdgeTocrafterConstraints(Crafter crafter, Dictionary<(Crafter, int), List<Edge>> edgeMap, Recipe.ItemAmount[] slots, Decision[] decidedAmounts) {
      for (var i = 0; i < slots.Length; i++) {
        var edges = edgeMap.GetValueOrDefault((crafter, i), empty);
        if (edges.Count == 0) continue; // TODO: figure out what to do about a disconnected input, and output
        var edgeSum = edges.Aggregate((Term)0, (term, e) => term + edgeAmounts[e]);
        model.AddConstraint(null, edgeSum == decidedAmounts[i]);
      }
    }

    // Add decision variables for each edge.
    foreach (var edge in Edges) {
      edgeAmounts[edge] = AddDecision(Domain.IntegerNonnegative, $"e_{edge}");
      edgeTimes[edge] = AddDecision(Domain.RealNonnegative, $"et_{edge}");
      outputEdges.GetOrAdd((edge.From, edge.OutputIdx), () => new()).Add(edge);
      inputEdges.GetOrAdd((edge.To, edge.InputIdx), () => new()).Add(edge);
    }

    // Set up the constraints for each crafter's input and output, and collect the decided output amounts.
    foreach (var crafter in Crafters) {
      var inputAmounts = crafter.Recipe.Inputs.Select(amount => AddMaxAmountDecision(crafter, amount)).ToArray();
      var outputAmounts = crafter.Recipe.Outputs.Select(amount => AddMaxAmountDecision(crafter, amount)).ToArray();
      AddCraftTimeConstraints(crafter, inputAmounts, outputAmounts);
      AddEdgeTocrafterConstraints(crafter, inputEdges, crafter.Recipe.Inputs, inputAmounts);
      AddEdgeTocrafterConstraints(crafter, outputEdges, crafter.Recipe.Outputs, outputAmounts);

      if (PlayerCraftRequests.TryGetValue(crafter, out var numCrafts)) {
        // Only need to constrain one output to ensure the crafter will craft it.
        if (crafter.Recipe.Outputs.Length > 0) {
          var outputsRequested = crafter.Recipe.Outputs[0].Count * numCrafts;
          model.AddConstraint(null, outputAmounts[0] >= outputsRequested);
        } else {
          var inputsRequested = crafter.Recipe.Inputs[0].Count * numCrafts;
          model.AddConstraint(null, inputAmounts[0] >= inputsRequested);
        }
      }
    }

    var solution = solver.Solve(new Directive() { TimeLimit = 5000 });

    foreach (var c in model.Constraints) {
      Debug.Log($"constraint: {c.Expression}");
    }
    foreach (var d in model.Decisions) {
      Debug.Log($"decision: {d.Name} == {d}");
    }

    foreach (var edge in Edges) {
      var amount = (int)edgeAmounts[edge].ToDouble();
      EdgeDemands[edge] = amount;
      if (amount > 0)
        edge.From.TryStartCrafting();
      Debug.Log($"Edge {edge} has demand {edgeAmounts[edge]}, time {edgeTimes[edge]}");
    }
    Debug.Log($"Total craft time {t}");
  }

  public void OnCraftFinished(Crafter crafter, int[] outputQueue) {
    if (PlayerCraftRequests.TryGetValue(crafter, out var numCrafts) && numCrafts > 0)
      PlayerCraftRequests[crafter] = numCrafts - 1;
    for (var i = 0; i < outputQueue.Length; i++) {
      var edgeDemand = EdgeDemands.FirstOrDefault(e => e.Key.From == crafter && e.Key.OutputIdx == i);
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
      if (obj != gameObject && obj.TryGetComponent(out Crafter crafter) && crafter == edge.To)
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
  Crafter NewCrafter(Recipe recipe, string name) {
    var go = new GameObject(name);
    go.SetActive(false);
    go.AddComponent<Animator>();
    go.AddComponent<BuildObject>();
    var m = go.AddComponent<Crafter>();
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
    if (!Application.isPlaying) {
      Debug.Log("Enter play mode first, this depends on creating objects");
      return;
    }
    var itemIron = ScriptableObject.CreateInstance<ItemInfo>();
    var itemGear = ScriptableObject.CreateInstance<ItemInfo>();
    var itemBelt = ScriptableObject.CreateInstance<ItemInfo>();
    var recipeMiner = NewRecipe(1f, new Recipe.ItemAmount[0], NewIngred(itemIron, 1));
    var recipeGear = NewRecipe(1f, new[] { NewIngred(itemIron, 3) }, NewIngred(itemGear, 1));
    var recipeBelt = NewRecipe(1f, new[] { NewIngred(itemGear, 1), NewIngred(itemIron, 1) }, NewIngred(itemBelt, 1));
    Crafters = new() {
      NewCrafter(recipeMiner, "ma"),
      NewCrafter(recipeMiner, "mb"),
      NewCrafter(recipeGear, "g"),
      NewCrafter(recipeBelt, "belt"),
    };
    Edges = new() {
      new() { From = Crafters[0], OutputIdx = 0, To = Crafters[2], InputIdx = 0 },
      new() { From = Crafters[1], OutputIdx = 0, To = Crafters[2], InputIdx = 0 },
      new() { From = Crafters[0], OutputIdx = 0, To = Crafters[3], InputIdx = 1 },
      new() { From = Crafters[1], OutputIdx = 0, To = Crafters[3], InputIdx = 1 },
      new() { From = Crafters[2], OutputIdx = 0, To = Crafters[3], InputIdx = 0 },
    };
    PlayerCraftRequests[Crafters[3]] = 1;

    SolveForRequests();

    Edges = new();
    Crafters.ForEach(m => Destroy(m.gameObject));
  }
#endif

}
