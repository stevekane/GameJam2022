using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BuildObject))]
public class Crafter : MonoBehaviour, IContainer, IInteractable {
  [ES3NonSerializable] public Recipe[] Recipes;
  public Recipe CurrentRecipe;

  // Arrays of input/output amounts held by this machine, in order of Recipe.Inputs/Outputs.
  [ShowInInspector, ES3Serializable] Dictionary<ItemProto, int> InputQueue = new();
  [ShowInInspector, ES3Serializable] Dictionary<ItemProto, int> OutputQueue = new();
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
    if (GetExtractCount(item) >= count is var enough && enough) {
      var remaining = OutputQueue[item] -= count;
      if (remaining == 0)
        OutputQueue.Remove(item);
    }
    return enough;
  }

  public int GetExtractCount(ItemProto item) => GetOutputQueue(item);

  public class RequestJob : Worker.Job {
    public Container From;
    public Crafter To;
    public ItemAmount Request;

    public override bool CanStart() => From.GetExtractCount(Request.Item) >= Request.Count;
    public override TaskFunc<Worker.Job> Run(Worker worker) => async scope => {
      await worker.MoveTo(scope, From.transform);
      if (!From.ExtractItem(Request.Item, Request.Count))
        return null;
      try {
        worker.Inventory.Add(Request.Item, Request.Count);
        return new DepositJob { Target = To };
      } catch {
        // If we fail to finish the dropoff, return it whence it came.
        return new DepositJob() { Target = From };
      } finally {
      }
    };
    public override void OnGUI() {
      GUIExtensions.DrawLabel(To.transform.position, $"Request:{Request.Item}:{Request.Count}");
    }
  }
  public class HarvestJob : Worker.Job {
    public Crafter Target;
    public ItemProto Item;

    public override bool CanStart() => true;
    public override TaskFunc<Worker.Job> Run(Worker worker) => async scope => {
      await worker.MoveTo(scope, Target.transform);
      int count = Target.GetOutputQueue(Item);
      if (count == 0 || !Target.ExtractItem(Item, count))
        return null;
      worker.Inventory.Add(Item, count);
      var hub = FindObjectOfType<Container>();
      return new DepositJob { Target = FindObjectOfType<Container>() }; // TODO
    };
    public override void OnGUI() {
      //var delta = To.Transform.position - From.Transform.position;
      //var pos = From.Transform.position + delta*.2f;
      //GUIExtensions.DrawLine(From.Transform.position, To.Transform.position, 2);
      GUIExtensions.DrawLabel(Target.transform.position, $"Harvest:{Item.name}");
    }
  }
  public class DepositJob : Worker.Job {
    public IContainer Target;
    Inventory DebugInventory = null;

    public override bool CanStart() => true;
    public override TaskFunc<Worker.Job> Run(Worker worker) => async scope => {
      DebugInventory = worker.Inventory;
      await worker.MoveTo(scope, Target.Transform);
      foreach (var kvp in worker.Inventory.Contents)
        Target.InsertItem(kvp.Key, kvp.Value);
      worker.Inventory.Contents.Clear();
      return null;
    };
    public override void OnGUI() {
      string ToString(Dictionary<ItemProto, int> queue) => string.Join("\n", queue.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"));
      GUIExtensions.DrawLabel(Target.Transform.position, $"Deposit:{ToString(DebugInventory.Contents)}");
    }
  }

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
      WorkerManager.Instance.AddJob(new RequestJob { From = hub, To = this, Request = input });
  }

  public void RequestHarvestOutput() {
    foreach (var output in OutputQueue) {
      if (!WorkerManager.Instance.GetAllJobs().Any(j => j is HarvestJob h && h.Target == this && h.Item == output.Key))
        WorkerManager.Instance.AddJob(new HarvestJob { Target = this, Item = output.Key });
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

  void Awake() {
    this.InitComponentFromChildren(out Animator, true);
    this.InitComponent(out BuildObject, true);
    GetComponent<SaveObject>().RegisterSaveable(this);
  }

  void OnDestroy() => CraftTask?.Dispose();

  bool JustLoaded = false;
  void FixedUpdate() {
    if (JustLoaded) {
      JustLoaded = false;
      RequestHarvestOutput();
      if (CurrentRecipe)
        RequestCraft();
    }
  }

#if UNITY_EDITOR
  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    string ToString(Dictionary<ItemProto, int> queue) => string.Join("\n", queue.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"));
    GUIExtensions.DrawLabel(InputPortPos, ToString(InputQueue));
    GUIExtensions.DrawLabel(OutputPortPos - transform.forward, ToString(OutputQueue));
  }
#endif
}