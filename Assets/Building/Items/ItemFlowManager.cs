using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;

// Represents an input or output item slot on a crafter.
using Slot = System.ValueTuple<Crafter, Recipe, int>;

// Concepts:
// CapillaryGroup: a connected set of capillaries, and which crafters are connected to it.
// CrafterSlot (Slot): an input/output slot that consumes/produces a single item for a given recipe.
// Edge: a (CapillaryGroup, Item) pair representing how much of an item is provided/consumed through this group.
// Each CrafterSlot connects to a single Edge, which may be shared with other CrafterSlots. The solver uses
// constraints to compute the crafting flow:
//   amount(Edge) = sum(output CrafterSlots)
//   amount(Edge) = sum(input CrafterSlots)
// Recipe constraints enforce the recipes consumption and production of ingredients in the proper ratios:
//   craftTime(CrafterSlot X) = craftTime(CrafterSlot Y), for each pair X,Y on a (Crafter,Recipe) pair.
// The final constraint adds the requested items:
//   amount(Edge X) = requestedAmount, where X is the output edge producing the item desired.
public class CraftSolver {
  class Edge {
    public int Index;
    public CapillaryGroup CapillaryGroup;
    public ItemInfo Item;
    public List<Slot> Producers = new();
    public List<Slot> Consumers = new();
    public override string ToString() => $"Edge:{Item.name}:{CapillaryGroup}";
  }

  public Dictionary<Slot, int> OutputSlotAmounts;
  public Dictionary<Slot, int> InputSlotAmounts;
  public Dictionary<Slot, int> CraftRequests = new();

  List<Edge> Edges;
  List<(Crafter, Recipe)> CrafterRecipes;

  public CapillaryGroup GetCapillaryGroupForInputSlot(Slot slot) {
    var edge = Edges.First(e => e.Consumers.Contains(slot));
    return edge.CapillaryGroup;
  }

  public Slot FindConsumerRequestingFromSlot(Slot producerSlot) {
    var edge = Edges.First(e => e.Producers.Contains(producerSlot));
    var result = InputSlotAmounts.FirstOrDefault(kv => kv.Value > 0 && edge.Consumers.Contains(kv.Key));
    if (result.Value > 0 && --InputSlotAmounts[result.Key] == 0) {
      InputSlotAmounts.Remove(result.Key);
    }
    return result.Key;  // can be null
  }

  public void OnBuildingsChanged() {
    CraftRequests = new();
  }

  public void AddCraftRequest(Slot slot) {
    CraftRequests[slot] = CraftRequests.GetValueOrDefault(slot) + 1;
    Profiler.BeginSample("craftflow build graph");
    CreateEdges();
    Profiler.EndSample();
    Profiler.BeginSample("craftflow constraints");
    SolveForRequests();
    Profiler.EndSample();
  }

  void CreateEdges() {
    BuildObjectManager.Instance.MaybeRefresh();
    Edges = new();
    CrafterRecipes = new();
    foreach ((var slot, int amount) in CraftRequests) {
      var (producer, producerRecipe, outputIdx) = slot;
      var outItem = producerRecipe.Outputs[outputIdx].Item;
      var outEdge = GetOrCreateEdge(producer.OutputCapillaryGroup, outItem, out var _);
      BuildEdgesFromSlot(slot, outEdge);
    }
  }

  void BuildEdgesFromSlot(Slot outSlot, Edge outEdge) {
    // Add the crafter producing on this edge to its producers.
    var (crafter, crafterRecipe, _) = outSlot;
    if (outEdge.Producers.Contains(outSlot)) {
      Debug.Log("TODO");
      return; ///????
    }
    outEdge.Producers.Add(outSlot);
    Debug.Assert(!CrafterRecipes.Contains((crafter, crafterRecipe)));
    CrafterRecipes.Add((crafter, crafterRecipe));

    // Add an edge for each of the crafter's inputs, adding it as a consumer to each edge.
    var inGroup = crafter.InputCapillaryGroup;
    for (var i = 0; i < crafterRecipe.Inputs.Length; i++) {
      var inItem = crafterRecipe.Inputs[i].Item;
      var inEdge = GetOrCreateEdge(inGroup, inItem, out var inExisted);
      var inSlot = (crafter, crafterRecipe, i);
      if (inEdge.Consumers.Contains(inSlot)) {
        Debug.Log("TODO");
        continue;  // ?????
      }
      inEdge.Consumers.Add((crafter, crafterRecipe, i));

      // Recursively follow this edge to its CapillaryGroup's producers, adding the edge's producers
      // and following their inputs in turn.
      if (!inExisted) {
        foreach (var producer in inGroup.Producers) {
          if (producer.FindRecipeProducing(inItem) is var producerRecipe && !producerRecipe) continue;
          int outputIdx = Array.FindIndex(producerRecipe.Outputs, output => output.Item == inItem);
          BuildEdgesFromSlot((producer, producerRecipe, outputIdx), inEdge);
        }
      }
    }
  }

  Edge GetEdge(CapillaryGroup group, ItemInfo item) => Edges.Find(e => e.CapillaryGroup == group && e.Item == item);
  Edge GetOrCreateEdge(CapillaryGroup group, ItemInfo item, out bool alreadyExisted) {
    if (GetEdge(group, item) is var edge && edge != null) {
      alreadyExisted = true;
      return edge;
    }
    alreadyExisted = false;
    return CreateEdge(group, item);
  }
  Edge CreateEdge(CapillaryGroup group, ItemInfo item) {
    var edge = new Edge { Index = Edges.Count, CapillaryGroup = group, Item = item };
    Edges.Add(edge);
    return edge;
  }

  public bool DebugModel = true;
  void SolveForRequests() {
    var crafterTimes = new Dictionary<Crafter, ArithExpr>();
    var edgeTimes = new Dictionary<Edge, ArithExpr>();
    var edgeAmounts = new Dictionary<Edge, ArithExpr>();
    var inputSlotAmounts = new Dictionary<Slot, IntExpr>();
    var outputSlotAmounts = new Dictionary<Slot, IntExpr>();

    Microsoft.Z3.Global.ToggleWarningMessages(true);

    using var ctx = new Context(new() { { "model", "true" } });
    var solver = ctx.MkOptimize();

    int didx = 0;
    string SymbolName(string name) => $"v{didx++}_{name}";
      //Regex.Replace(Regex.Replace($"v{didx++}_{name}", "\\([^)]*\\)", ""), "  *", " ");

    RealExpr AddReal(string name) {
      var x = ctx.MkRealConst(SymbolName(name));
      solver.Assert(x >= 0);
      //solver.Assert(x <= 10);
      return x;
    }

    IntExpr AddInt(string name) {
      var x = ctx.MkIntConst(SymbolName(name));
      solver.Assert(x >= 0);
      return x;
    }

    var empty = new List<Edge>();
    // Adds variables for the amount of an item consumed/produced at each crafter Slot.
    void AddSlotAmountVars(Crafter crafter, Recipe recipe, Recipe.ItemAmount[] slots, Dictionary<Slot, IntExpr> slotAmounts, string name) {
      for (var i = 0; i < slots.Length; i++)
        slotAmounts[(crafter, recipe, i)] = AddInt($"slot{name}:{crafter.name}:{recipe.name}:{i}");
    }
    // Adds constraints ensuring that recipe ratios are honored (e.g. consumes 2 iron for every 1 gear produced).
    void AddRecipeRatioConstraints(Crafter crafter, Recipe recipe) {
      ArithExpr TimeToCraft(int idx, Recipe.ItemAmount[] amounts, Dictionary<Slot, IntExpr> slotAmounts) =>
        ctx.MkInt2Real(slotAmounts[(crafter, recipe, idx)]) / (amounts[idx].Count / recipe.CraftTime);

      // Recipe ratios: time spent on each slot is the same.
      var craftTime =
        recipe.Inputs.Length > 0 ? TimeToCraft(0, recipe.Inputs, inputSlotAmounts) :
        recipe.Outputs.Length > 0 ? TimeToCraft(0, recipe.Outputs, outputSlotAmounts) : null;
      for (var i = 0; i < recipe.Inputs.Length; i++)
        solver.Assert(ctx.MkEq(TimeToCraft(i, recipe.Inputs, inputSlotAmounts), craftTime));
      for (var i = 0; i < recipe.Outputs.Length; i++)
        solver.Assert(ctx.MkEq(TimeToCraft(i, recipe.Outputs, outputSlotAmounts), craftTime));

      if (crafterTimes.ContainsKey(crafter)) {
        crafterTimes[crafter] += craftTime;
      } else {
        crafterTimes[crafter] = craftTime;
      }
    }
    void AddCraftTimeConstraints(Crafter crafter, ArithExpr crafterTime) {
      // Cumulative craft times = total time from system start for this crafter to finish crafting.
      var cumCraftTime = AddReal($"ct_{crafter.name}");
      var inEdgeTime = edgeTimes.Where(kv => kv.Key.CapillaryGroup == crafter.InputCapillaryGroup)
        .Aggregate((ArithExpr)ctx.MkReal(0),
          (term, kv) => (ArithExpr)ctx.MkITE(term > kv.Value, term, kv.Value));
      solver.Assert(ctx.MkEq(cumCraftTime, crafterTime + inEdgeTime));
      foreach ((var edge, var edgeTime) in edgeTimes) {
        var prod = edge.Producers.FirstOrDefault(slot => CraftRequests.ContainsKey(slot));
        if (prod.Item1 == crafter)
          solver.MkMinimize(cumCraftTime);
      }
    }

    // Add the constraints for each crafter's input and output.
    foreach (var (crafter, recipe) in CrafterRecipes) {
      AddSlotAmountVars(crafter, recipe, recipe.Inputs, inputSlotAmounts, "in");
      AddSlotAmountVars(crafter, recipe, recipe.Outputs, outputSlotAmounts, "out");
      AddRecipeRatioConstraints(crafter, recipe);
    }

    // Add constraints that the number of items on each edge is equal to both the number produced by crafters onto the edge
    // and the number consumed by crafters from the edge.
    foreach (var edge in Edges) {
      var amount = edgeAmounts[edge] = AddInt($"{edge}");
      var producerSum = edge.Producers.Aggregate((ArithExpr)ctx.MkInt(0), (term, c) => term + outputSlotAmounts[c]);
      solver.Assert(ctx.MkEq(amount, producerSum));
      if (edge.Consumers.Count > 0) {
        var consumerSum = edge.Consumers.Aggregate((ArithExpr)ctx.MkInt(0), (term, c) => term + inputSlotAmounts[c]);
        solver.Assert(ctx.MkEq(amount, consumerSum));
      }
    }

    // Add constraints that the amount output on the requested slots is the requested amount.
    foreach (var (slot, amount) in CraftRequests) {
      solver.Assert(ctx.MkEq(outputSlotAmounts[slot], ctx.MkInt(amount)));
    }

    // Add craft time constraints to minimize total time spent crafting.
    // Confusion1: constraining edges before crafters seems to be faster for some reason?
    // Confusion2: really the edges should be using cumulative crafterTime, but using only the immediate producers crafterTime
    // seems to work as well, and is way faster.
    foreach (var edge in Edges) {
      var edgeTime = edgeTimes[edge] = AddReal($"t{edge}");
      var maxCrafterTime = edge.Producers.Aggregate((ArithExpr)ctx.MkReal(0),
        (term, c) => (ArithExpr)ctx.MkITE(term > crafterTimes[c.Item1], term, crafterTimes[c.Item1]));
      solver.Assert(ctx.MkEq(edgeTime, maxCrafterTime));
    }
    foreach (var (crafter, crafterTime) in crafterTimes) {
      AddCraftTimeConstraints(crafter, crafterTime);
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
      Debug.Log($"Model: {solver.Model}");
    }

    OutputSlotAmounts = new();
    foreach ((var slot, var amountVar) in outputSlotAmounts) {
      var amount = (int)solver.Model.Double(amountVar);
      if (amount > 0) {
        OutputSlotAmounts[slot] = amount;
        Debug.Log($"Crafter {slot.Item1.name} outputs {amount} {slot.Item2.Outputs[slot.Item3].Item.name}");
      }
    }
    InputSlotAmounts = new();
    foreach ((var slot, var amountVar) in inputSlotAmounts) {
      var amount = (int)solver.Model.Double(amountVar);
      if (amount > 0) {
        InputSlotAmounts[slot] = amount;
        Debug.Log($"Crafter {slot.Item1.name} inputs {amount} {slot.Item2.Inputs[slot.Item3].Item.name}");
      }
    }
  }
}

public class ItemFlowManager : MonoBehaviour {
  public static ItemFlowManager Instance;

  class ItemFlow {
    public ItemObject Object;
    public Vector3 NextWorldPos;
    public List<Vector2Int> Path;
    public Slot ConsumerSlot;

    public Vector2Int NextCell => BuildGrid.WorldToGrid(NextWorldPos);
  }

  List<ItemFlow> Items = new();
  CraftSolver CraftSolver = new();

  public void OnBuildingsChanged() {
    CraftSolver.OnBuildingsChanged();
    Items = new();
  }
  public void AddCraftRequest(Crafter crafter) {
    var slot = (crafter, crafter.Recipes[0], 0);
    CraftSolver.AddCraftRequest(slot);
    StartCrafting();
  }
  public void AddCraftRequest(Crafter crafter, Recipe recipe) {
    var slot = (crafter, recipe, 0);
    CraftSolver.AddCraftRequest(slot);
    StartCrafting();
  }

  void StartCrafting() {
    // Inform all crafters of the requested amounts first, then have them start outputting. Crafters may have
    // multiple recipes to craft and will be able to prioritize with the full request information.
    foreach (((var crafter, var recipe, var outputIdx), var amount) in CraftSolver.OutputSlotAmounts) {
      crafter.SetOutputRequest(recipe.Outputs[outputIdx].Item, amount);
    }
    foreach (((var crafter, var recipe, var outputIdx), var amount) in CraftSolver.OutputSlotAmounts) {
      crafter.CheckRequestSatisfied();
    }
  }

  public void OnOutputReady(Crafter crafter, Recipe recipe, ItemInfo item) {
    int outputIdx = Array.FindIndex(recipe.Outputs, i => i.Item == item);
    SpawnOutput((crafter, recipe, outputIdx), item);
  }

  void SpawnOutput(Slot slot, ItemInfo item) {
    if (!CraftSolver.OutputSlotAmounts.ContainsKey(slot)) {
      Debug.Log("what");
    }
    CraftSolver.OutputSlotAmounts[slot]--;
    if (CraftSolver.CraftRequests.GetValueOrDefault(slot) > 0) {
      if (--CraftSolver.CraftRequests[slot] == 0)
        CraftSolver.CraftRequests.Remove(slot);
    }

    var producer = slot.Item1;
    var outputCell = producer.OutputPortCell;
    //Debug.Assert(!IsCellOccupied(outputCell, item), "Figure out how to handle this case");
    producer.ExtractOutput(item);

    var instance = item.Spawn(producer.transform.position);
    var consumerSlot = CraftSolver.FindConsumerRequestingFromSlot(slot);
    var flow = new ItemFlow {
      Object = instance,
      Path = ComputePath(outputCell, consumerSlot),
      ConsumerSlot = consumerSlot,
    };
    AdvanceOnPath(flow);
    Items.Add(flow);

    producer.CheckRequestSatisfied();
  }

  List<Vector2Int> ComputePath(Vector2Int startCell, Slot consumerSlot) {
    var consumer = consumerSlot.Item1;
    if (!consumer)
      return new() { startCell };

    var destCell = consumer.InputPortCell;
    var cells = CraftSolver.GetCapillaryGroupForInputSlot(consumerSlot).Cells;
    var toVisit = new Queue<Vector2Int>();
    var distance = new Dictionary<Vector2Int, int>();
    var prev = new Dictionary<Vector2Int, Vector2Int>();
    toVisit.Enqueue(startCell);
    distance[startCell] = 0;
    while (toVisit.TryDequeue(out var pos)) {
      if (!cells.Contains(pos)) continue;
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
    List<Vector2Int> path = new();
    for (var pos = destCell; pos != startCell; pos = prev[pos])
      path.Add(pos);
    path.Add(startCell);
    path.Reverse();
    path.Add(BuildGrid.WorldToGrid(consumer.transform.position));
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
    foreach (var flow in toConsume) {
      (var consumer, var recipe, var inputIdx) = flow.ConsumerSlot;
      if (consumer)
        consumer.InsertInput(recipe.Inputs[inputIdx].Item);
      flow.Object.gameObject.Destroy();
    }
    toConsume.ForEach(f => Items.Remove(f));
  }

  void AdvanceOnPath(ItemFlow item) {
    if (IsCellOccupied(item.Path[0], item.Object.Info)) return;
    item.NextWorldPos = BuildGrid.GridToWorld(item.Path[0], item.Object.transform.position.y);
    item.Path.RemoveAt(0);
  }

  void FixedUpdate() {
    MoveItems();
  }

#if false
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