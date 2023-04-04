using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;

// Represents an input or output item slot on a crafter.
using Slot = System.ValueTuple<Crafter, Recipe, int>;

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
  Dictionary<ItemInfo, int> PlayerCraftRequests = new();
  Dictionary<Edge, int> EdgeDemands = new();

  public void AddCraftRequest(ItemInfo item) {
    PlayerCraftRequests[item] = PlayerCraftRequests.GetValueOrDefault(item) + 1;
    Profiler.BeginSample("craftflow buildgraph");
    BuildGraph();
    Profiler.EndSample();
    Profiler.BeginSample("craftflow buildconstraints");
    SolveForRequests();
    Profiler.EndSample();
  }

  struct Edge {
    public Slot From;
    public Slot To;

    public Crafter FromCrafter => From.Item1;
    public Recipe FromRecipe => From.Item2;
    public int OutputIdx => From.Item3;
    public Recipe.ItemAmount OutputIngredient => FromRecipe.Outputs[OutputIdx];
    public Crafter ToCrafter => To.Item1;
    public Recipe ToRecipe => To.Item2;
    public int InputIdx => To.Item3;
    public Recipe.ItemAmount InputIngredient => ToRecipe.Inputs[InputIdx];

    public bool IsNull() => From.Item1 == null;

    public override int GetHashCode() => AsTuple().GetHashCode();
    public override bool Equals(object other) => other is Edge e && AsTuple().Equals(e.AsTuple());
    public override string ToString() => $"[{From.Item1?.name}:{From.Item2?.name} => {To.Item1?.name}:{To.Item2?.name}]";
    (Crafter, Recipe, int, Crafter, Recipe, int) AsTuple() => (From.Item1, From.Item2, From.Item3, To.Item1, To.Item2, To.Item3);
  }
  List<Edge> Edges;
  List<Crafter> Crafters;
  List<(Crafter, Recipe)> CrafterRecipes;
  Dictionary<Edge, Vector2Int> EdgeOutputCells = new();
  Dictionary<Crafter, List<(Crafter, Vector2Int)>> Connections = new();

  void BuildGraph() {
    Edges = new();
    CrafterRecipes = new();
    Crafters = FindObjectsOfType<Crafter>().ToList();  // TODO: cache this
    Connections = new();
    foreach (var c in Crafters)
      Connections[c] = c.FindConnectedCrafters().ToList();
    var connected = Crafters.Select(c => (c, Vector2Int.zero));
    foreach ((var item, int amount) in PlayerCraftRequests) {
      BuildEdgesFromInputs((null, null, -1), connected, item);
    }
  }

  void BuildEdgesFromInputs(Slot to, IEnumerable<(Crafter, Vector2Int)> producerCandidates, ItemInfo item) {
    foreach ((var producer, var outputCell) in producerCandidates) {
      if (producer.FindRecipeProducing(item) is var producerRecipe && !producerRecipe) continue;
      int outputIdx = Array.FindIndex(producerRecipe.Outputs, output => output.Item == item);
      var edge = new Edge { From = (producer, producerRecipe, outputIdx), To = to };
      // TODO: is it ever possible to have this edge already?
      if (!Edges.Contains(edge)) {
        Edges.Add(edge);
        EdgeOutputCells[edge] = outputCell;
      }
      if (!CrafterRecipes.Contains((producer, producerRecipe))) {
        CrafterRecipes.Add((producer, producerRecipe));
      }
      for (int i = 0; i < producerRecipe.Inputs.Length; i++) {
        // TODO: cache connected
        BuildEdgesFromInputs((producer, producerRecipe, i), Connections[producer], producerRecipe.Inputs[i].Item); 
      }
    }
  }

  // TODO: clean this up ffs
  // This uses a constraint solver to compute how much of each item each crafter should attempt to produce, and what items it
  // needs to consume to do so. The end goal is to minimize total crafting time in the system.
  // The approach relies on two main concepts:
  // - Slot: a single item input or output on a crafter (including which recipe it belongs to).
  // - Edge: a path for a single item from a producing crafter's output slot to its consumer crafter's input slot.
  //         alternatively, this is a pair of slots (producer, consumer) aka (from, to).
  // We build constraints on the slots and edges and the link between them:
  // RecipeRatio constraint:
  //   time to produce/consume every item demanded at SlotX == time to produce/consume every item at SlotY,
  //   foreach SlotX and SlotY on a given (crafter, recipe) pair
  // EdgeToSlot constraint:
  //   item amount demanded at SlotX == sum(edge amounts connected to SlotX)
  // EdgeTime constraints:
  //   time to satsify demand on Edge e == sum(machine craft times from start, to producer of item on edge e)
  //   alternatively, time(e) == e.producer's craft time + max(time(inputEdge) foreach inputEdge connected to e.producer)
  public bool DebugModel = false;
  void SolveForRequests() {
    var edgeAmounts = new Dictionary<Edge, ArithExpr>();
    var edgeTimes = new Dictionary<Edge, ArithExpr>();
    var crafterTimes = new Dictionary<Crafter, ArithExpr>();
    var inputEdges = new Dictionary<Slot, List<Edge>>();
    var outputEdges = new Dictionary<Slot, List<Edge>>();

    Microsoft.Z3.Global.ToggleWarningMessages(true);

    using var ctx = new Context(new() { { "model", "true" } });
    var solver = ctx.MkOptimize();

    int didx = 0;
    string SymbolName(string name) =>
      Regex.Replace(Regex.Replace($"d{didx++}_{name}", "\\([^)]*\\)", ""), "  *", " ");
    RealExpr AddReal(string name) {
      var x = ctx.MkRealConst(SymbolName(name));
      solver.Assert(x >= 0);
      return x;
    }
    IntExpr AddInt(string name) {
      var x = ctx.MkIntConst(SymbolName(name));
      solver.Assert(x >= 0);
      solver.Assert(x <= 10);
      return x;
    }

    // Main goal: minimize time spent crafting.
    var t = AddReal("t");
    var tMin = solver.MkMinimize(t);

    var empty = new List<Edge>();
    // Adds constraint on the maximum that can consumed/produced in the given time.
    IntExpr AddSlotAmountVar(Crafter crafter, Recipe.ItemAmount amount) {
      var decidedAmount = AddInt($"c_{crafter.name}_{amount.Item.name}");
      return decidedAmount;
    }
    // Adds constraints on the craft time of the crafter and the total craft time to deliver each output (edgeTimes[e]).
    void AddCraftTimeConstraints(Crafter crafter, Recipe recipe, IntExpr[] inputSlotAmounts, IntExpr[] outputSlotAmounts) {
      ArithExpr TimeToCraft(int idx, IntExpr[] decidedAmounts, Recipe.ItemAmount[] amounts) =>
        ctx.MkInt2Real(decidedAmounts[idx]) / (amounts[idx].Count / recipe.CraftTime);
      var craftTime =
        inputSlotAmounts.Length > 0 ? TimeToCraft(0, inputSlotAmounts, recipe.Inputs) :
        outputSlotAmounts.Length > 0 ? TimeToCraft(0, outputSlotAmounts, recipe.Outputs) : null;

      // Recipe ratios: time spent on each slot is the same.
      for (var i = 0; i < inputSlotAmounts.Length; i++)
        solver.Assert(ctx.MkEq(TimeToCraft(i, inputSlotAmounts, recipe.Inputs), craftTime));
      for (var i = 0; i < outputSlotAmounts.Length; i++)
        solver.Assert(ctx.MkEq(TimeToCraft(i, outputSlotAmounts, recipe.Outputs), craftTime));

      crafterTimes[crafter] = crafterTimes.GetValueOrDefault(crafter, ctx.MkInt(0)) + craftTime;
    }
    void AddCraftTimeConstraints2(Crafter crafter, ArithExpr crafterTime) {
      // Edge craft times = total time from system start to satisfy the given edge's full demand.
      // Note: this is not strictly correct. If a crafter has 2 output edges, it might satisfy the first edge at time T,
      // then the second at time T + T2. But this is a good enough approximation for our purposes - minimizing total
      // craft time.
      var totalCraftTime = AddReal($"ct_{crafter.name}");
      solver.Assert(totalCraftTime >= crafterTime);
      foreach ((var edge, var edgeTime) in edgeTimes) {
        // Input edge constraint: totalCraftTime >= sum(crafter's craft times) + max(totalTime(inputEdges)))
        if (edge.ToCrafter == crafter)
          solver.Assert(totalCraftTime >= crafterTime + edgeTimes[edge]);
        // Output edge constraint: totalTime(outputEdge) = totalCraftTime
        if (edge.FromCrafter == crafter)
          solver.Assert(ctx.MkEq(edgeTime, totalCraftTime));
      }
    }
    // Adds constraint: sum(edges attached to crafter slot N) = crafter's slot_N ArithExpr.
    void AddEdgeToCrafterConstraints(Crafter crafter, Recipe recipe, Dictionary<Slot, List<Edge>> edgeMap, ArithExpr[] decidedAmounts) {
      for (var i = 0; i < decidedAmounts.Length; i++) {
        var edges = edgeMap.GetValueOrDefault((crafter, recipe, i), empty);
        if (edges.Count == 0) continue; // TODO: figure out what to do about a disconnected input, and output
        var edgeSum = edges.Aggregate((ArithExpr)ctx.MkInt(0), (term, e) => term + edgeAmounts[e]);
        solver.Assert(ctx.MkEq(edgeSum, decidedAmounts[i]));
      }
    }

    // Add decision variables for each edge.
    foreach (var edge in Edges) {
      edgeAmounts[edge] = AddInt($"e_{edge}");
      edgeTimes[edge] = AddReal($"et_{edge}");
      outputEdges.GetOrAdd(edge.From, () => new()).Add(edge);
      inputEdges.GetOrAdd(edge.To, () => new()).Add(edge);

      // Add constraint for the edge producing a requested item.
      if (PlayerCraftRequests.TryGetValue(edge.OutputIngredient.Item, out var desiredAmount)) {
        solver.Assert(edgeAmounts[edge] >= desiredAmount);
        solver.Assert(t >= edgeTimes[edge]);
      }
    }

    // Set up the constraints for each crafter's input and output, and collect the decided output amounts.
    foreach (var (crafter, recipe) in CrafterRecipes) {
      var inputSlotAmounts = recipe.Inputs.Select(amount => AddSlotAmountVar(crafter, amount)).ToArray();
      var outputSlotAmounts = recipe.Outputs.Select(amount => AddSlotAmountVar(crafter, amount)).ToArray();
      //AddCraftTimeConstraints(crafter, recipe, inputSlotAmounts.Select(i => ctx.MkInt2Real(i)).ToArray(), outputSlotAmounts.Select(i => ctx.MkInt2Real(i)).ToArray());
      AddCraftTimeConstraints(crafter, recipe, inputSlotAmounts, outputSlotAmounts);
      AddEdgeToCrafterConstraints(crafter, recipe, inputEdges, inputSlotAmounts);
      AddEdgeToCrafterConstraints(crafter, recipe, outputEdges, outputSlotAmounts);
    }
    foreach (var (crafter, crafterTime) in crafterTimes) {
      AddCraftTimeConstraints2(crafter, crafterTime);
    }

    Debug.Log($"Version {Microsoft.Z3.Version.FullVersion}");
    Debug.Log($"Numbers: edges={Edges.Count} crafterRecipes={CrafterRecipes.Count} constraints={solver.Assertions.Length}");
    if (DebugModel) {
      foreach (var constraint in solver.Assertions) {
        Debug.Log($"Constraint: {constraint}");
      }
    }

    Profiler.BeginSample("craftflow solveZ3");
    var solution = solver.Check();
    Profiler.EndSample();

    if (DebugModel) {
      Debug.Log($"Model: t={t} {solver.Model}");
    }

    foreach (var edge in Edges) {
      var amount = EdgeDemands[edge] = (int)solver.Model.Double(edgeAmounts[edge]);
      if (amount > 0)
        edge.FromCrafter.SetOutputRequest(edge.OutputIngredient.Item, amount);
      if (amount > 0)
        Debug.Log($"Edge {edge} has demand {amount}, time {solver.Model.Double(edgeTimes[edge])}");
    }
    foreach (var edge in Edges) {
      if (EdgeDemands[edge] > 0)
        edge.FromCrafter.CheckRequestSatisfied();
    }
  }

  // TODO clean this shit up, move to Crafter?
  public void OnOutputReady(Crafter crafter, ItemInfo item) {
    var edgeDemand = EdgeDemands.FirstOrDefault(e => e.Key.FromCrafter == crafter && e.Key.OutputIngredient.Item == item && e.Value > 0);
    Debug.Assert(!edgeDemand.Key.IsNull() && edgeDemand.Value > 0);
    if (!edgeDemand.Key.IsNull())
      SpawnOutput(edgeDemand.Key);
  }

  void SpawnOutput(Edge edge) {
    var outputCell = EdgeOutputCells[edge];
    var item = edge.OutputIngredient.Item;
    Debug.Assert(!IsCellOccupied(outputCell, item), "Figure out how to handle this case");
    edge.FromCrafter.ExtractOutput(item);
    if (PlayerCraftRequests.GetValueOrDefault(item) > 0)
      PlayerCraftRequests[item]--;

    var instance = item.Spawn(edge.FromCrafter.transform.position);
    var flow = new ItemFlow {
      Object = instance,
      Path = ComputePath(outputCell, edge),
      Edge = edge,
    };
    AdvanceOnPath(flow);
    Items.Add(flow);

    edge.FromCrafter.CheckRequestSatisfied();
    if (--EdgeDemands[edge] == 0)
      EdgeDemands.Remove(edge);
  }

  List<Vector2Int> ComputePath(Vector2Int startCell, Edge edge) {
    var destCell = startCell;
    var y = edge.FromCrafter.transform.position.y;
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
      if (obj != gameObject && obj.TryGetComponent(out Crafter crafter) && crafter == edge.ToCrafter)
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
      if (item.Edge.ToCrafter)
        item.Edge.ToCrafter.InsertInput(item.Edge.InputIngredient.Item);
      item.Object.gameObject.Destroy();
    }
    toConsume.ForEach(f => Items.Remove(f));
  }

  void AdvanceOnPath(ItemFlow item) {
    if (IsCellOccupied(item.Path[0], item.Object.Info)) return;
    item.NextWorldPos = BuildGrid.GridToWorld(item.Path[0], item.Edge.FromCrafter.transform.position.y);
    item.Path.RemoveAt(0);
  }

  void FixedUpdate() {
    MoveItems();
  }

#if true
  Crafter NewCrafter(Recipe recipe, string name) => NewCrafter(new[] { recipe }, name);
  Crafter NewCrafter(Recipe[] recipes, string name) {
    var go = new GameObject(name);
    go.SetActive(false);
    go.AddComponent<Animator>();
    go.AddComponent<BuildObject>();
    var m = go.AddComponent<Crafter>();
    m.Recipes = recipes;
    go.SetActive(true);
    return m;
  }
  Recipe NewRecipe(string name, float craftTime, Recipe.ItemAmount[] inputs, Recipe.ItemAmount output) {
    var recipe = ScriptableObject.CreateInstance<Recipe>();
    recipe.name = name;
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
  ItemInfo NewItem(string name) {
    var item = ScriptableObject.CreateInstance<ItemInfo>();
    item.name = name;
    return item;
  }
  [ContextMenu("Test flow 1")]
  void Test1() {
    if (!Application.isPlaying) {
      Debug.Log("Enter play mode first, this depends on creating objects");
      return;
    }
    var itemIron = NewItem("iron");
    var itemGear = NewItem("gear");
    var itemBelt = NewItem("belt");
    var recipeMiner = NewRecipe("miner", 1f, new Recipe.ItemAmount[0], NewIngred(itemIron, 1));
    var recipeGear = NewRecipe("gear", 1f, new[] { NewIngred(itemIron, 3) }, NewIngred(itemGear, 1));
    var recipeBelt = NewRecipe("belt", 1f, new[] { NewIngred(itemIron, 1), NewIngred(itemGear, 1) }, NewIngred(itemBelt, 1));
    Crafters = new() {
      NewCrafter(recipeMiner, "ma"),
      NewCrafter(recipeMiner, "mb"),
      NewCrafter(recipeGear, "g"),
      NewCrafter(recipeBelt, "belt"),
    };
    Edges = new() {
      new() { From = (Crafters[0], recipeMiner, 0), To = (Crafters[2], recipeGear, 0) },
      new() { From = (Crafters[1], recipeMiner, 0), To = (Crafters[2], recipeGear, 0) },
      new() { From = (Crafters[0], recipeMiner, 0), To = (Crafters[3], recipeBelt, 0) },
      new() { From = (Crafters[1], recipeMiner, 0), To = (Crafters[3], recipeBelt, 0) },
      new() { From = (Crafters[2], recipeGear, 0), To = (Crafters[3], recipeBelt, 1) },
      new() { From = (Crafters[3], recipeBelt, 0), To = (null, null, -1) },
    };
    PlayerCraftRequests[itemBelt] = 1;

    CrafterRecipes = new();
    foreach (var crafter in Crafters) {
      foreach (var recipe in crafter.Recipes) {
        CrafterRecipes.Add((crafter, recipe));
      }
    }

    SolveForRequests();

    Edges = new();
    Crafters.ForEach(m => Destroy(m.gameObject));
  }

  [ContextMenu("Test flow 2")]
  void Test2() {
    if (!Application.isPlaying) {
      Debug.Log("Enter play mode first, this depends on creating objects");
      return;
    }
    var itemIron = NewItem("iron");
    var itemGear = NewItem("gear");
    var itemBelt = NewItem("belt");
    var itemSplitter = NewItem("splitter");
    var recipeMiner = NewRecipe("miner",1f, new Recipe.ItemAmount[0], NewIngred(itemIron, 1));
    var recipeGear = NewRecipe("gear", 1f, new[] { NewIngred(itemIron, 2) }, NewIngred(itemGear, 1));
    var recipeBelt = NewRecipe("belt", 1f, new[] { NewIngred(itemIron, 2) }, NewIngred(itemBelt, 1));
    var recipeSplitter = NewRecipe("splitter", 1f, new[] { NewIngred(itemGear, 1), NewIngred(itemBelt, 1) }, NewIngred(itemSplitter, 1));
    Crafters = new() {
      NewCrafter(recipeMiner, "i"),
      NewCrafter(new[] {recipeGear, recipeBelt }, "gb"),
      NewCrafter(recipeSplitter, "splitter"),
    };
    Edges = new() {
      new() { From = (Crafters[0], recipeMiner, 0), To = (Crafters[1], recipeGear, 0) },
      new() { From = (Crafters[0], recipeMiner, 0), To = (Crafters[1], recipeBelt, 0) },
      new() { From = (Crafters[1], recipeGear, 0), To = (Crafters[2], recipeSplitter, 0) },
      new() { From = (Crafters[1], recipeBelt, 0), To = (Crafters[2], recipeSplitter, 1) },
      new() { From = (Crafters[2], recipeSplitter, 0), To = (null, null, -1) },
    };
    PlayerCraftRequests[itemSplitter] = 1;

    CrafterRecipes = new();
    foreach (var crafter in Crafters) {
      foreach (var recipe in crafter.Recipes) {
        CrafterRecipes.Add((crafter, recipe));
      }
    }

    SolveForRequests();

    Edges = new();
    Crafters.ForEach(m => Destroy(m.gameObject));
  }

  [ContextMenu("Test Z3")]
  void TestZ3() {
    Microsoft.Z3.Global.ToggleWarningMessages(true);

    using (Context ctx = new Context(new Dictionary<string, string>() { { "model", "true" } })) {
      var solver = ctx.MkOptimize();

      var t = ctx.MkRealConst("t");
      var m1i = ctx.MkIntConst("m1i feb");
      var m2i = ctx.MkIntConst("m2i (wee)");
      var m3i = ctx.MkIntConst("m3i");
      var m1o = ctx.MkIntConst("m1o");
      var m2o = ctx.MkIntConst("m2o");
      var m3o = ctx.MkIntConst("m3o");

      var mint = solver.MkMinimize(t);

      solver.Assert(t >= 0);
      // connections
      solver.Assert(ctx.MkEq(m1i, 2 * t));
      solver.Assert(ctx.MkEq(m2i, 2 * t));
      solver.Assert(ctx.MkEq(m3i, m1o + m2o));
      // constraints: machine input and output rates
      solver.Assert(m1i <= 2 * t);
      solver.Assert(m1o <= 1 * t);
      solver.Assert(m2i <= 2 * t);
      solver.Assert(m2o <= 1 * t);
      solver.Assert(m3i <= 2 * t);
      solver.Assert(m3o <= 1 * t);
      // constraints: input/output rates match
      solver.Assert(ctx.MkEq(m1i / 2, m1o / 1));
      solver.Assert(ctx.MkEq(m2i / 2, m2o / 1));
      solver.Assert(ctx.MkEq(m3i / 2, m3o / 1));
      solver.Assert(ctx.MkEq(m1o, ctx.MkInt(1)));

      if (Microsoft.Z3.Status.SATISFIABLE == solver.Check()) {
        Debug.Log($"{solver.Model}");
        Debug.Log($"opt: {mint} val={mint.Value}");
      } else {
        Debug.Log("BUG, the constraints are satisfiable.");
      }
    }
  }
#endif
}