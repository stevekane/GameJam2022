using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Represents an input or output item slot on a crafter.
using Slot = System.ValueTuple<Crafter, Recipe, int>;

public class ItemFlowManager : MonoBehaviour {
  public static ItemFlowManager Instance;

  class ItemFlow {
    public ItemObject Object;
    public Vector3 NextWorldPos;
    public CapillaryGroup CapillaryGroup;
    public List<Vector2Int> Path;
    public Slot ConsumerSlot;

    public Vector2Int PrevCell;
    public Vector2Int NextCell => BuildGrid.WorldToGrid(NextWorldPos);
  }

  List<ItemFlow> Items = new();
  CraftSolver CraftSolver = new();
  bool NeedsRebuild = true;
  bool NeedsSolve = false;

  public void AddCraftRequest(Crafter crafter, Recipe recipe) {
    var slot = (crafter, recipe, 0);
    CraftSolver.AddCraftRequest(slot);
    NeedsSolve = true;
    Debug.Log($"Add craft: {crafter}, {recipe}");
  }

  public void OnBuildingsChanged() {
    Debug.Log($"Buildings Changed");
    NeedsRebuild = true;
    NeedsSolve = true;
  }

  void RebuildGraph() {
    CraftSolver.OnBuildingsChanged();
    BuildObjectManager.Instance.MaybeRefresh();
    List<ItemFlow> ToRemove = new();
    foreach (var item in Items) {
      var startCell = item.PrevCell;
      item.CapillaryGroup =
        BuildObjectManager.Instance.GetCapillaryGroup(startCell = item.PrevCell) ??
        BuildObjectManager.Instance.GetCapillaryGroup(startCell = item.NextCell);
      if (item.CapillaryGroup == null) {
        Debug.Log($"Item not on cap group anymore: {item.Object.Info.name}");
        ToRemove.Add(item);
      } else {
        if (item.ConsumerSlot.Item1 == null) {
          // Target consumer was destroyed (or we never had one).
          item.ConsumerSlot = CraftSolver.FindConsumerRequestingItem(item.CapillaryGroup, item.Object.Info);
        }
        item.Path = ComputePath(item.CapillaryGroup, startCell, item.ConsumerSlot);
        item.NextWorldPos = item.Object.transform.position;
        AdvanceOnPath(item);
      }
    }
    ToRemove.ForEach(i => { i.Object.gameObject.Destroy(); Items.Remove(i); });
  }

  void StartCrafting() {
    CraftSolver.SolveForRequests(Items.Select(i => i.ConsumerSlot));

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
    var capillaryGroup = consumerSlot.Item1 ? CraftSolver.GetCapillaryGroupForInputSlot(consumerSlot) : null;
    var flow = new ItemFlow {
      Object = instance,
      Path = ComputePath(capillaryGroup, outputCell, consumerSlot),
      NextWorldPos = instance.transform.position,
      ConsumerSlot = consumerSlot,
      CapillaryGroup = capillaryGroup,
    };
    AdvanceOnPath(flow);
    Items.Add(flow);

    producer.CheckRequestSatisfied();
  }

  List<Vector2Int> ComputePath(CapillaryGroup capillaryGroup, Vector2Int startCell, Slot consumerSlot) {
    if (capillaryGroup == null)
      return new() { startCell };

    var consumer = consumerSlot.Item1;
    var destCell = consumer.InputPortCell;
    var cells = capillaryGroup.Cells;
    if (!cells.Contains(destCell))
      return new() { startCell };
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
    item.PrevCell = item.NextCell;
    item.NextWorldPos = BuildGrid.GridToWorld(item.Path[0], item.Object.transform.position.y);
    item.Path.RemoveAt(0);
  }

  void FixedUpdate() {
    if (NeedsRebuild) {
      NeedsRebuild = false;
      RebuildGraph();
    }
    if (NeedsSolve) {
      NeedsSolve = false;
      StartCrafting();
    }
    MoveItems();
  }

#if UNITY_EDITOR
  public bool DebugDraw = false;
  void OnGUI() {
    if (!DebugDraw)
      return;

    Vector2 PathPointToGUI(ItemFlow item, int pathIdx) {
      var worldPos = BuildGrid.GridToWorld(item.Path[pathIdx], item.Object.transform.position.y);
      return Camera.main.WorldToGUIPoint(worldPos);
    }
    foreach (var item in Items) {
      for (int i = 0; i < item.Path.Count-1; i++) {
        var startPos = PathPointToGUI(item, i);
        var endPos = PathPointToGUI(item, i+1);
        GUIExtensions.DrawLine(startPos, endPos, 3);
      }
    }
  }
#endif

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