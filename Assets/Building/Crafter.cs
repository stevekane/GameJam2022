using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BuildObject))]
public class Crafter : MonoBehaviour, IContainer, IInteractable {
  [ES3NonSerializable] public Recipe[] Recipes;
  public Recipe CurrentRecipe;
  [ES3Serializable] int CraftTicksRemaining = -1;

  Inventory Inventory;
  TaskScope CraftTask;
  TaskTimer CraftTimer;
  ItemObject RecipeIndicator;
  Animator Animator;
  BuildObject BuildObject;

  // Inputs and Outputs are held in the inventory. Inputs are those in CurrentRecipe.Inputs, outputs are everything
  // else (and include extra items left over after switching recipes).
  IEnumerable<KeyValuePair<ItemProto, int>> InputQueue =>
    Inventory.Contents.Where(i => CurrentRecipe?.Inputs.Any(ia => ia.Item == i.Key) ?? false);
  IEnumerable<KeyValuePair<ItemProto, int>> OutputQueue =>
    Inventory.Contents.Where(i => !CurrentRecipe?.Inputs.Any(ia => ia.Item == i.Key) ?? true);

  // IInteractable
  public string[] Choices => Recipes.Select(r => r.name).ToArray();
  public void Choose(Character interacter, int choiceIdx) {
    SetRecipe(Recipes[choiceIdx]);
  }
  public void Rotate(float degrees) {
    transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
  }
  public void Deposit(Character interacter) {
    var characterInventory = interacter.GetComponent<Inventory>();
    var hasAllInputs = CurrentRecipe != null && CurrentRecipe.Inputs.All(input => characterInventory.Count(input.Item) >= input.Count);
    if (!hasAllInputs) return;
    CancelRequestJobs();
    foreach (var input in CurrentRecipe.Inputs)
      characterInventory.MoveTo(Inventory, input.Item, input.Count);
    CraftIfSatisfied();
  }
  public void Collect(Character interacter) {
    // Cancel worker jobs trying to fetch crafter outputs.
    WorkerManager.Instance.CancelJobsIf(j => j is CollectJob h && h.Target == this);
    var characterInventory = interacter.GetComponent<Inventory>();
    foreach ((var outputItem, var count) in OutputQueue.ToArray())
      Inventory.MoveTo(characterInventory, outputItem, count);
  }

  void SetRecipe(Recipe recipe) {
    CancelRequestJobs();
    CurrentRecipe = recipe;
    RecipeIndicator?.gameObject?.Destroy();
    if (CurrentRecipe) {
      if (CurrentRecipe.Outputs.Length > 0)
        RecipeIndicator = CurrentRecipe.Outputs[0].Item.Spawn(transform.position + 3f*Vector3.up);
      RequestCraft();
    }
  }

  //public Vector2Int InputPortCell => BuildGrid.WorldToGrid(InputPortPos);
  //public Vector2Int OutputPortCell => BuildGrid.WorldToGrid(OutputPortPos);
  public Vector3 InputPortPos => transform.position - transform.rotation*new Vector3(BuildGrid.GetBottomLeftOffset(BuildObject.Size).y + 1f, 0f, 0f);
  public Vector3 OutputPortPos => transform.position + transform.rotation*new Vector3(BuildGrid.GetTopRightOffset(BuildObject.Size).y + 1f, 0f, 0f);

  // IContainer
  public Transform Transform => transform;

  // Adds an item to the input queue, which could possibly trigger a craft.
  public bool InsertItem(ItemProto item, int count) {
    Inventory.Add(item, count);
    CraftIfSatisfied();
    return true;
  }

  // Removes an item from the output queue.
  public bool ExtractItem(ItemProto item, int count) {
    if (Inventory.Count(item) >= count is var enough && enough)
      Inventory.Remove(item, count);
    return enough;
  }

  public int GetExtractCount(ItemProto item) => Inventory.Count(item);

  // See if we can output an item or begin a craft that has been requested, and do it if so.
  public void CraftIfSatisfied() {
    if (CraftTask != null) return;
    var satisfied = CurrentRecipe.Inputs.All(i => Inventory.Count(i.Item) >= i.Count);
    if (satisfied) {
      var craftTime = CraftTicksRemaining >= 0 ? Timeval.FromTicks(CraftTicksRemaining) : Timeval.FromSeconds(CurrentRecipe.CraftTime);
      CraftTicksRemaining = -1;
      CraftTask = TaskScope.StartNew(s => Craft(s, CurrentRecipe, craftTime));
    }
  }

  public void RequestCraft() {
    CraftIfSatisfied();
    if (CraftTask != null) return;

    var hub = FindObjectOfType<Container>();
    foreach (var input in CurrentRecipe.Inputs)
      WorkerManager.Instance.AddJob(new RequestJob { From = hub, To = this, Request = input });
  }

  public void RequestCollectOutput() {
    foreach (var output in OutputQueue) {
      if (!WorkerManager.Instance.GetAllJobs().Any(j => j is CollectJob h && h.Target == this && h.Item == output.Key))
        WorkerManager.Instance.AddJob(new CollectJob { Target = this, Item = output.Key });
    }
  }

  // Cancel worker jobs trying to give this crafter items.
  void CancelRequestJobs() {
    WorkerManager.Instance.CancelJobsIf(j =>
      j is RequestJob r && r.To == this ||
      j is DepositJob d && d.Target == (IContainer)this);
  }

  void CancelCrafterJobs() {
    WorkerManager.Instance.CancelJobsIf(j =>
      j is RequestJob r && r.To == this ||
      j is DepositJob d && d.Target == (IContainer)this ||
      j is CollectJob c && c.Target == this);
  }

  // TODO: this is no good for save/load
  async Task Craft(TaskScope scope, Recipe recipe, Timeval craftTime) {
    bool finished = false;
    var isBuildPlot = GetComponent<BuildPlot>();
    try {
      using var disposeTask = CraftTask;
      await scope.Until(() => OutputQueue.Sum(kvp => kvp.Value) < 10);
      Animator.SetBool("Crafting", true);
      CraftTimer = new(craftTime);
      await CraftTimer.WaitDone(scope);
      foreach (var input in recipe.Inputs)
        Inventory.Remove(input.Item, input.Count);
      foreach (var output in recipe.Outputs)
        Inventory.Add(output.Item, output.Count);
      finished = true;
    } finally {
      Animator?.SetBool("Crafting", false);
      CraftTask = null;
      CraftTimer = null;
      if (finished)
        OnCraftFinished(recipe);
    }
  }

  void OnCraftFinished(Recipe recipe) {
    foreach (var output in recipe.Outputs)
      output.Item.OnCrafted(this);
  }

  void OnBeforeSave() {
    CraftTicksRemaining = CraftTimer != null ? CraftTimer.TicksRemaining : -1;
  }

  void Awake() {
    this.InitComponentFromChildren(out Inventory);
    this.InitComponentFromChildren(out Animator, true);
    this.InitComponent(out BuildObject, true);
    GetComponent<SaveObject>().RegisterSaveable(this);
  }

  void Start() {
    SetRecipe(CurrentRecipe);
  }

  void OnDestroy() {
    CancelCrafterJobs();
    CraftTask?.Dispose();
    RecipeIndicator?.gameObject?.Destroy();
  }

  bool JustLoaded = false;
  void FixedUpdate() {
    if (JustLoaded) {
      JustLoaded = false;
      RequestCollectOutput();
      if (CurrentRecipe)
        RequestCraft();
    }
  }

  int guiIdx = 0;
#if UNITY_EDITOR
  void OnGUI() {
    guiIdx = 0;
    if (!WorkerManager.Instance.DebugDraw)
      return;
    string ToString(IEnumerable<KeyValuePair<ItemProto, int>> queue) =>
      string.Join("\n", queue.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"));
    GUIExtensions.DrawLabel(transform.position, ToString(InputQueue));
    GUIExtensions.DrawLabel(OutputPortPos - transform.forward, ToString(OutputQueue));
    GUIExtensions.DrawLabel(transform.position + 2*Vector3.up, CraftTimer != null ? $"remaining={CraftTimer.TicksRemaining}" : "");
  }
#endif

  // Job telling a worker to fetch an input item from a container. Once the worker has the item,
  // a new DepositJob continues to deliver the item to us.
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
        Debug.Log($"Crafter: Request job was cancelled.");
        // If we fail to finish the dropoff, return it whence it came.
        return new DepositJob() { Target = From };
      } finally {
      }
    };
    public override void OnGUI() {
      var newlines = string.Concat(Enumerable.Repeat("\n", To.guiIdx++));
      var blue = new Color(.3f, .3f, 1, 1);
      GUIExtensions.DrawLabel(To.transform.position, $"{newlines}r:{Request.Item.name}:{Request.Count}", blue);
    }
  }
  // Job telling a worker to collect items from our output queue. Once the worker has the items,
  // a new DepositJob continues to drop the item off in a container.
  public class CollectJob : Worker.Job {
    public Crafter Target;
    public ItemProto Item;

    public override bool CanStart() => true;
    public override TaskFunc<Worker.Job> Run(Worker worker) => async scope => {
      await worker.MoveTo(scope, Target.transform);
      int count = Target.GetExtractCount(Item);
      if (count == 0 || !Target.ExtractItem(Item, count))
        return null;
      worker.Inventory.Add(Item, count);
      var hub = FindObjectOfType<Container>();
      return new DepositJob { Target = FindObjectOfType<Container>() }; // TODO
    };
    public override void OnGUI() {
      var newlines = string.Concat(Enumerable.Repeat("\n", Target.guiIdx++));
      var red = new Color(1, .3f, .3f, 1);
      GUIExtensions.DrawLabel(Target.transform.position, $"{newlines}c:{Item.name}", red);
    }
  }
  // Job telling the worker to deposit his items in a target (container or our crafter).
  public class DepositJob : Worker.Job {
    public IContainer Target;
    Inventory DebugInventory = null;

    public override bool CanStart() => true;
    public override TaskFunc<Worker.Job> Run(Worker worker) => async scope => {
      try {
        DebugInventory = worker.Inventory;
        await worker.MoveTo(scope, Target.Transform);
        foreach (var kvp in worker.Inventory.Contents)
          Target.InsertItem(kvp.Key, kvp.Value);
        worker.Inventory.Contents.Clear();
        return null;
      } catch {
        // If we fail to finish the dropoff, return it to a container.
        if (Target is Crafter && FindObjectOfType<Container>() is var container && container != null) { // TODO: container
          Debug.Log($"Crafter: Deposit job cancelled, returning to container");
          return new DepositJob { Target = container };
        }
        return null;
      } finally {
      }
    };
    public override void OnGUI() {
      string ToString(Dictionary<ItemProto, int> queue) => string.Join("\n", queue.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"));
      GUIExtensions.DrawLabel(Target.Transform.position, $"d:{ToString(DebugInventory.Contents)}");
    }
  }
}