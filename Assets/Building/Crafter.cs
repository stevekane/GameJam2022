using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static SaveObject;

[RequireComponent(typeof(BuildObject))]
public class Crafter : MonoBehaviour, IContainer, IInteractable, ISaveableComponent {
  public Recipe[] Recipes;
  public Recipe CurrentRecipe;

  // Arrays of input/output amounts held by this machine, in order of Recipe.Inputs/Outputs.
  Dictionary<ItemProto, int> InputQueue = new();
  Dictionary<ItemProto, int> OutputQueue = new();
  TaskScope CraftTask;
  ItemObject CraftDisplayObject;
  Animator Animator;
  BuildObject BuildObject;

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
  public Vector3 InputPortPos => transform.position - transform.rotation*new Vector3(0f, 0f, BuildGrid.GetBottomLeftOffset(BuildObject.Size).y + 1f);
  public Vector3 OutputPortPos => transform.position + transform.rotation*new Vector3(0f, 0f, BuildGrid.GetTopRightOffset(BuildObject.Size).y + 1f);

  public int GetInputQueue(ItemProto item) => InputQueue.GetValueOrDefault(item);
  public int GetOutputQueue(ItemProto item) => OutputQueue.GetValueOrDefault(item);

  // IContainer
  public Transform Transform => transform;

  // Adds an item to the input queue, which could possibly trigger a craft.
  public bool InsertItem(ItemProto item, int count) {
    InputQueue[item] = InputQueue.GetValueOrDefault(item) + count;
    CraftIfSatisfied();
    return true;
  }

  // Removes an item from the output queue.
  public bool ExtractItem(ItemProto item, int count) {
    if (GetExtractCount(item) >= count is var enough && enough)
      OutputQueue[item] -= count;
    return enough;
  }

  public int GetExtractCount(ItemProto item) => GetOutputQueue(item);

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
    CraftIfSatisfied();
  }

  // See if we can output an item or begin a craft that has been requested, and do it if so.
  public void CraftIfSatisfied() {
    if (CraftTask != null) return;
    var satisfied = CurrentRecipe.Inputs.All(i => InputQueue.GetValueOrDefault(i.Item) >= i.Count);
    if (satisfied)
      CraftTask = TaskScope.StartNew(s => Craft(s, CurrentRecipe));
  }

  public void RequestCraft() {
    CraftIfSatisfied();
    if (CraftTask != null) return;

    var hub = FindObjectOfType<Container>();
    foreach (var input in CurrentRecipe.Inputs)
      WorkerManager.Instance.AddDeliveryJob(hub, this, input);
  }

  public void RequestHarvestOutput() {
    var hub = FindObjectOfType<Container>();  // TODO: atm I'm assuming one container
    foreach (var output in CurrentRecipe.Outputs) {
      WorkerManager.Instance.AddDeliveryJob(this, hub, output);
    }
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
    foreach (var output in recipe.Outputs)
      output.Item.OnCrafted(this);
  }

  SaveObject SaveObject;
  public ILoadableComponent Save() => new Serialized {
    CurrentRecipe = CurrentRecipe,
    // TODO: input/output queue. (Serializable)Dictionary does not work with ItemInfo?
    // TODO: crafting progress
  };
  class Serialized : ILoadableComponent {
    public Recipe CurrentRecipe;
    public void Load(GameObject go) {
      go.GetComponent<Crafter>().CurrentRecipe = CurrentRecipe;
    }
  }

  void Awake() {
    this.InitComponentFromChildren(out Animator);
    this.InitComponent(out BuildObject);
    this.InitComponent(out SaveObject);
    SaveObject.RegisterSaveable(this);
  }

  void OnDestroy() => CraftTask?.Dispose();

#if UNITY_EDITOR
  public string[] DebugInputs;
  public string[] DebugOutputs;
  void FixedUpdate() {
    IEnumerable<string> ToList(Dictionary<ItemProto, int> queue) {
      foreach ((var item, int amount) in queue) {
        if (amount > 0)
          yield return $"{item.name}:{amount}";
      }
    }
    string[] ToArray(Dictionary<ItemProto, int> queue) => ToList(queue).ToArray();
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