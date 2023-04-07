using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BuildObject))]
public class Crafter : MonoBehaviour {
  public Recipe[] Recipes;

  public CapillaryGroup InputCapillaryGroup { get; set; }
  public CapillaryGroup OutputCapillaryGroup { get; set; }

  // Arrays of input/output amounts held by this machine, in order of Recipe.Inputs/Outputs.
  Dictionary<ItemInfo, int> InputQueue = new();
  Dictionary<ItemInfo, int> OutputQueue = new();
  Dictionary<ItemInfo, int> OutputRequests = new();
  TaskScope CraftTask;
  ItemObject CraftDisplayObject;
  Animator Animator;
  BuildObject BuildObject;

  // Note: We assume there is only 1 recipe that can produce a given output.
  public Recipe FindRecipeProducing(ItemInfo item) => Recipes.FirstOrDefault(r => r.Outputs.Any(o => o.Item == item));

  public Vector2Int InputPortCell =>
    BuildGrid.WorldToGrid(transform.position - transform.rotation*new Vector3(0f, 0f, BuildGrid.GetBottomLeftOffset(BuildObject).y + 1f));
  public Vector2Int OutputPortCell =>
    BuildGrid.WorldToGrid(transform.position + transform.rotation*new Vector3(0f, 0f, BuildGrid.GetBottomLeftOffset(BuildObject).y + 1f));

  public void SetOutputRequest(ItemInfo item, int amount) {
    OutputRequests[item] = amount;
  }

  // Adds an item to the input queue, which could possibly trigger a craft.
  public void InsertInput(ItemInfo item) {
    InputQueue[item] = InputQueue.GetValueOrDefault(item) + 1;
    CheckRequestSatisfied();
  }

  // Removes an item from the output queue.
  public void ExtractOutput(ItemInfo item) {
    Debug.Assert(OutputQueue.GetValueOrDefault(item) > 0);
    OutputQueue[item]--;
    if (OutputRequests.GetValueOrDefault(item) > 0) {
      if (--OutputRequests[item] == 0)
        OutputRequests.Remove(item);
    }
  }

  // See if we can output an item or begin a craft that has been requested, and do it if so.
  public void CheckRequestSatisfied() {
    if (CraftTask != null) return;
    Recipe recipeToCraft = null;
    ItemInfo itemToOutput = null;
    foreach (var (item, amount) in OutputRequests) {
      var recipe = FindRecipeProducing(item);
      if (OutputQueue.GetValueOrDefault(item) > 0) {
        (recipeToCraft, itemToOutput) = (recipe, item);
        break;  // prefer outputting items we already have
      }
      var satisfied = recipe.Inputs.All(i => InputQueue.GetValueOrDefault(i.Item) >= i.Count);
      if (satisfied)
        (recipeToCraft, itemToOutput) = (recipe, item);
    }
    if (recipeToCraft != null) 
      CraftTask = TaskScope.StartNew(s => Craft(s, recipeToCraft, itemToOutput));
  }

  async Task Craft(TaskScope scope, Recipe recipe, ItemInfo item) {
    bool finished = false;
    try {
      using var disposeTask = CraftTask;
      if (OutputQueue.GetValueOrDefault(item) == 0) {
        Animator.SetBool("Crafting", true);
        CraftDisplayObject = recipe.Outputs[0].Item.Spawn(transform.position + 3f*Vector3.up);
        foreach (var input in recipe.Inputs) {
          Debug.Assert(InputQueue[input.Item] > 0);
          InputQueue[input.Item] -= input.Count;
        }
        await scope.Seconds(recipe.CraftTime);
        foreach (var output in recipe.Outputs)
          OutputQueue[output.Item] = OutputQueue.GetValueOrDefault(output.Item) + output.Count;
      }
      finished = true;
    } finally {
      Animator.SetBool("Crafting", false);
      CraftDisplayObject?.gameObject.Destroy();
      CraftTask = null;
      if (finished)
        ItemFlowManager.Instance.OnOutputReady(this, recipe, item);
    }
  }

  public IEnumerable<(Crafter, Vector2Int)> FindConnectedCrafters() {
    var center = BuildGrid.WorldToGrid(transform.position);
    var y = transform.position.y;
    var toVisit = new Queue<(Vector2Int, Vector2Int)>();
    var visited = new HashSet<Vector2Int>();
    toVisit.Enqueue((center, Vector2Int.zero));
    while (toVisit.TryDequeue(out var visit)) {
      (var pos, var fromPos) = visit;
      if (visited.Contains(pos)) continue;
      visited.Add(pos);
      var obj = BuildGrid.GetCellContents(pos, y);
      if (obj == null) continue;
      if (obj == gameObject || obj.TryGetComponent(out Capillary _)) {
        toVisit.Enqueue((pos + Vector2Int.left, pos));
        toVisit.Enqueue((pos + Vector2Int.right, pos));
        toVisit.Enqueue((pos + Vector2Int.up, pos));
        toVisit.Enqueue((pos + Vector2Int.down, pos));
      }
      if (obj != gameObject && obj.TryGetComponent(out Crafter machine))
        yield return (machine, fromPos);
    }
  }

  void Awake() {
    this.InitComponentFromChildren(out Animator);
    this.InitComponent(out BuildObject);
  }

  void OnDestroy() => CraftTask?.Dispose();

#if UNITY_EDITOR
  public string[] DebugInputs;
  public string[] DebugOutputs;
  public string[] DebugRequests;
  void FixedUpdate() {
    IEnumerable<string> ToList(Dictionary<ItemInfo, int> queue) {
      foreach ((var item, int amount) in queue) {
        if (amount > 0)
          yield return $"{item}:{amount}";
      }
    }
    string[] ToArray(Dictionary<ItemInfo, int> queue) => ToList(queue).ToArray();
    DebugInputs = ToArray(InputQueue);
    DebugOutputs = ToArray(OutputQueue);
    DebugRequests = ToArray(OutputRequests);
    //string ToString(Dictionary<ItemInfo, int> queue) => string.Join(",", ToList(queue));
    //DebugUI.Log(this, $"{name}: in={ToString(InputQueue)}, out={ToString(OutputQueue)}, requested={ToString(OutputRequests)}");
  }
#endif
}