using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

// Represents an input or output item slot on a crafter.
using Slot = System.ValueTuple<Crafter, Recipe, int>;

public static class SlotExtensions {
  public static ItemInfo ItemAsInput(this Slot slot) => slot.Item2.Inputs[slot.Item3].Item;
  public static ItemInfo ItemAsOutput(this Slot slot) => slot.Item2.Outputs[slot.Item3].Item;
}

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
[Serializable]
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
  // Items currently in the system, keyed by input slot.
  Dictionary<Slot, int> ItemsInTransit;

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

  public Slot FindConsumerRequestingItem(CapillaryGroup group, ItemInfo item) {
    var result = InputSlotAmounts.FirstOrDefault(
      kv => kv.Value > 0 &&
      kv.Key.ItemAsInput() == item &&
      Edges.Any(e => e.CapillaryGroup == group && e.Item == item && e.Consumers.Contains(kv.Key)));
    if (result.Value > 0 && --InputSlotAmounts[result.Key] == 0) {
      InputSlotAmounts.Remove(result.Key);
    }
    return result.Key;  // can be null
  }


  public void OnBuildingsChanged() {
    //bool Check(Dictionary<Slot, int> amounts) => amounts?.Any(kvp => kvp.Key.Item1 == null) ?? false;
    //Check(OutputSlotAmounts);
    //Check(InputSlotAmounts);
    //Check(CraftRequests);
    while (CraftRequests.FirstOrDefault(kvp => kvp.Key.Item1 == null) is var staleRequest && staleRequest.Value > 0) {
      CraftRequests.Remove(staleRequest.Key);
    }
    CreateEdges();
  }

  public void AddCraftRequest(Slot slot) {
    CraftRequests[slot] = CraftRequests.GetValueOrDefault(slot) + 1;
    Edges = null;
  }

  public void SolveForRequests(IEnumerable<Slot> itemsInTransit) {
    if (Edges == null) {
      Profiler.BeginSample("craftflow build graph");
      CreateEdges();
      Profiler.EndSample();
    }

    ItemsInTransit = new();
    itemsInTransit.ForEach(s => ItemsInTransit[s] = ItemsInTransit.GetValueOrDefault(s) + 1);
    //foreach ((var slot, var amount) in ItemsInTransit) {
    //  Debug.Log($"In-transit: crafter {slot.Item1.name} expecting {amount} {slot.ItemAsInput().name}");
    //}
    foreach ((var crafter, var recipe) in CrafterRecipes) {
      for (int i = 0; i < recipe.Inputs.Length; i++) {
        if (crafter.GetInputQueue(recipe.Inputs[i].Item) is var amount && amount > 0) {
          var slot = (crafter, recipe, i);
          ItemsInTransit[slot] = ItemsInTransit.GetValueOrDefault(slot) + amount;
          //Debug.Log($"crafter {slot.Item1.name} input holding {amount} {recipe.Inputs[i].Item.name}");
        }
      }
      //for (int i = 0; i < recipe.Outputs.Length; i++) {
      //  if (crafter.GetOutputQueue(recipe.Outputs[i].Item) is var amount && amount > 0) {
      //    Debug.Log($"crafter {crafter.name} output holding {amount} {recipe.Outputs[i].Item.name}");
      //  }
      //  if (crafter.GetOutputRequests(recipe.Outputs[i].Item) is var amountReq && amountReq > 0) {
      //    Debug.Log($"crafter {crafter.name} output wants to make {amountReq} {recipe.Outputs[i].Item.name}");
      //  }
      //}
    }
    Profiler.BeginSample("craftflow constraints");
    SolveInternal();
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

  void SolveInternal() {
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
    RealExpr AddReal(string name) {
      var x = ctx.MkRealConst(SymbolName(name));
      solver.Assert(x >= 0);
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
      for (var i = 0; i < slots.Length; i++) {
        var amount = slotAmounts[(crafter, recipe, i)] = AddInt($"slot{name}:{crafter.name}:{recipe.name}:{i}");
        var amountInQueue = slots == recipe.Inputs ? crafter.GetInputQueue(slots[i].Item) : crafter.GetOutputRequests(slots[i].Item);
        solver.Assert(amount >= amountInQueue);
      }
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
      var producerSum = edge.Producers.Aggregate((ArithExpr)ctx.MkInt(0), (term, slot) => term + outputSlotAmounts[slot]);
      solver.Assert(ctx.MkEq(amount, producerSum));
      if (edge.Consumers.Count > 0) {
        var consumerSum = edge.Consumers.Aggregate((ArithExpr)ctx.MkInt(0), (term, slot) => term + inputSlotAmounts[slot] - ItemsInTransit.GetValueOrDefault(slot));
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

    //Debug.Log($"Numbers: edges={Edges.Count} crafterRecipes={CrafterRecipes.Count} constraints={solver.Assertions.Length}");
    //foreach (var constraint in solver.Assertions) {
    //  Debug.Log($"Constraint: {constraint}");
    //}

    Profiler.BeginSample("craftflow solveZ3");
    var solution = solver.Check();
    Profiler.EndSample();

    //Debug.Log($"Model: {solver.Model}");

    OutputSlotAmounts = new();
    foreach ((var slot, var amountVar) in outputSlotAmounts) {
      var amount = (int)solver.Model.Double(amountVar);
      if (amount > 0) {
        OutputSlotAmounts[slot] = amount;
        //Debug.Log($"Crafter {slot.Item1.name} outputs {amount} {slot.ItemAsOutput().name}");
      }
    }
    InputSlotAmounts = new();
    foreach ((var slot, var amountVar) in inputSlotAmounts) {
      var amount = (int)solver.Model.Double(amountVar);
      if (amount > 0) {
        var inTransit = ItemsInTransit.GetValueOrDefault(slot);
        InputSlotAmounts[slot] = amount - inTransit;
        //Debug.Log($"Crafter {slot.Item1.name} inputs {amount} {slot.ItemAsInput().name} ({inTransit} en route)");
      }
    }
  }
}