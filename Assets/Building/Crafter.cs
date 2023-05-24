using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BuildObject))]
public class Crafter : MonoBehaviour, IContainer, IInteractable {
  public Recipe[] Recipes;

  // Arrays of input/output amounts held by this machine, in order of Recipe.Inputs/Outputs.
  Dictionary<ItemInfo, int> InputQueue = new();
  Dictionary<ItemInfo, int> OutputQueue = new();
  TaskScope CraftTask;
  ItemObject CraftDisplayObject;
  Animator Animator;
  BuildObject BuildObject;
  Recipe CurrentRecipe;

  // IInteractable
  public string[] Choices => Recipes.Select(r => r.name).ToArray();
  public void Choose(Character interacter, int choiceIdx) {
    // TODO: cancel pending jobs
    CurrentRecipe = Recipes[choiceIdx];
    var inventory = interacter.GetComponent<Inventory>();
    if (CanCraft(inventory)) {
      TransferItems(inventory);
    } else {
      RequestCraft();
    }
  }
  public void Rotate(float degrees) {
    transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
  }

  // Note: We assume there is only 1 recipe that can produce a given output.
  //public Recipe FindRecipeProducing(ItemInfo item) => Recipes.FirstOrDefault(r => r.Outputs.Any(o => o.Item == item));

  //public Vector2Int InputPortCell => BuildGrid.WorldToGrid(InputPortPos);
  //public Vector2Int OutputPortCell => BuildGrid.WorldToGrid(OutputPortPos);
  Vector3 InputPortPos => transform.position - transform.rotation*new Vector3(0f, 0f, BuildGrid.GetBottomLeftOffset(BuildObject.Size).y + 1f);
  Vector3 OutputPortPos => transform.position + transform.rotation*new Vector3(0f, 0f, BuildGrid.GetTopRightOffset(BuildObject.Size).y + 1f);

  public int GetInputQueue(ItemInfo item) => InputQueue.GetValueOrDefault(item);
  public int GetOutputQueue(ItemInfo item) => OutputQueue.GetValueOrDefault(item);

  // IContainer
  public Transform Transform => transform;

  // Adds an item to the input queue, which could possibly trigger a craft.
  public bool InsertItem(ItemInfo item, int count) {
    InputQueue[item] = InputQueue.GetValueOrDefault(item) + count;
    CheckRequestSatisfied();
    return true;
  }

  // Removes an item from the output queue.
  public bool ExtractItem(ItemInfo item, int count) {
    if (GetExtractCount(item) >= count is var enough && enough) {
      OutputQueue[item] -= count;
      RequestCraft();
    }
    return enough;
  }

  public int GetExtractCount(ItemInfo item) => GetOutputQueue(item);

  public bool CanCraft(Inventory inventory) {
    if (!CurrentRecipe) return false;
    foreach (var input in CurrentRecipe.Inputs) {
      if (inventory.Count(input.Item) < input.Count)
        return false;
    }
    return true;
  }

  public void TransferItems(Inventory inventory) {
    Debug.Log($"Transferring items from {inventory.gameObject.name} to {name} for {CurrentRecipe}");
    foreach (var input in CurrentRecipe.Inputs) {
      inventory.Remove(input.Item, input.Count);
      // TODO: queue not needed?
      InputQueue[input.Item] = InputQueue.GetValueOrDefault(input.Item) + input.Count;
    }
    CheckRequestSatisfied();
  }

  // See if we can output an item or begin a craft that has been requested, and do it if so.
  public void CheckRequestSatisfied() {
    if (CraftTask != null) return;
    var satisfied = CurrentRecipe.Inputs.All(i => InputQueue.GetValueOrDefault(i.Item) >= i.Count);
    if (satisfied)
      CraftTask = TaskScope.StartNew(s => Craft(s, CurrentRecipe));
  }

  public void RequestCraft() {
    CheckRequestSatisfied();
    if (CraftTask != null) return;

    var hub = FindObjectOfType<Container>();
    foreach (var input in CurrentRecipe.Inputs)
      WorkerManager.Instance.AddDeliveryJob(hub, this, input);
  }

  // TODO: this is no good for save/load
  async Task Craft(TaskScope scope, Recipe recipe) {
    bool finished = false;
    var isBuildPlot = GetComponent<BuildPlot>();
    try {
      using var disposeTask = CraftTask;
      await scope.Until(() => OutputQueue.Sum(kvp => kvp.Value) <= 1);
      Animator.SetBool("Crafting", true);
      CraftDisplayObject = recipe.Outputs[0].Item.Spawn(transform.position + 3f*Vector3.up);
      await scope.Seconds(recipe.CraftTime);
      foreach (var input in recipe.Inputs) {
        Debug.Assert(InputQueue[input.Item] > 0);
        InputQueue[input.Item] -= input.Count;
      }
      foreach (var output in recipe.Outputs)
        OutputQueue[output.Item] = OutputQueue.GetValueOrDefault(output.Item) + output.Count;
      //if (!isBuildPlot)
      //  await scope.Until(() => ItemFlowManager.Instance.CanSpawnOutput(item, OutputPortCell));
      finished = true;
    } finally {
      CraftDisplayObject?.gameObject.Destroy();
      Animator?.SetBool("Crafting", false);
      CraftTask = null;
      if (finished)
        OnCraftFinished(recipe);
    }
  }

  void OnCraftFinished(Recipe recipe) {
    if (GetComponent<BuildPlot>() != null) {
      Debug.Assert(recipe.Outputs.Length == 1);
      recipe.Outputs[0].Item.Spawn(transform.position, transform.rotation);
      gameObject.Destroy();
      return;
    }
    var hub = FindObjectOfType<Container>();
    foreach (var output in CurrentRecipe.Outputs) {
      WorkerManager.Instance.AddDeliveryJob(this, hub, output);
    }
    RequestCraft();
  }

  void Awake() {
    this.InitComponentFromChildren(out Animator);
    this.InitComponent(out BuildObject);
  }

  void OnDestroy() => CraftTask?.Dispose();

#if UNITY_EDITOR
  public string[] DebugInputs;
  public string[] DebugOutputs;
  void FixedUpdate() {
    IEnumerable<string> ToList(Dictionary<ItemInfo, int> queue) {
      foreach ((var item, int amount) in queue) {
        if (amount > 0)
          yield return $"{item.name}:{amount}";
      }
    }
    string[] ToArray(Dictionary<ItemInfo, int> queue) => ToList(queue).ToArray();
    DebugInputs = ToArray(InputQueue);
    DebugOutputs = ToArray(OutputQueue);
  }

  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    string ToString(string[] list) => string.Join("\n", list);
    GUIExtensions.DrawLabel(InputPortPos, ToString(DebugInputs));
    GUIExtensions.DrawLabel(OutputPortPos - transform.forward, ToString(DebugOutputs));
  }
#endif
}