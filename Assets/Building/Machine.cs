using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BuildObject))]
public class Machine : MonoBehaviour {
  public Recipe Recipe;

  // Arrays of input/output amounts held by this machine, in order of Recipe.Inputs/Outputs.
  int[] InputQueue;
  int[] OutputQueue;
  TaskScope CraftTask;
  Animator Animator;

  public void TryStartCrafting() {
    CheckInputQueue();
  }

  public void InsertItem(int i) {
    InputQueue[i]++;
    CheckInputQueue();
  }

  public bool TryOutputItem(int i) {
    if (OutputQueue[i] > 0) {
      OutputQueue[i]--;
      return true;
    }
    return false;
  }

  void CheckInputQueue() {
    var satisfied = Enumerable.Range(0, Recipe.Inputs.Length).All(i => InputQueue[i] >= Recipe.Inputs[i].Count);
    if (satisfied && CraftTask == null) {
      CraftTask = TaskScope.StartNew(Craft);
    }
  }

  async Task Craft(TaskScope scope) {
    bool finished = false;
    try {
      using var disposeTask = CraftTask;
      Animator.SetBool("Crafting", true);
      for (var i = 0; i < InputQueue.Length; i++)
        InputQueue[i] -= Recipe.Inputs[i].Count;
      await scope.Seconds(Recipe.CraftTime);
      for (var i = 0; i < OutputQueue.Length; i++)
        OutputQueue[i] += Recipe.Outputs[i].Count;
      finished = true;
    } finally {
      Animator.SetBool("Crafting", false);
      CraftTask = null;
      if (finished)
        ItemFlowManager.Instance.OnCraftFinished(this, OutputQueue);
    }
  }


  public IEnumerable<(Machine, Vector2Int)> FindConnectedMachines() {
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
      if (obj != gameObject && obj.TryGetComponent(out Machine machine))
        yield return (machine, fromPos);
    }
  }

  void Awake() {
    this.InitComponentFromChildren(out Animator);
    InputQueue = new int[Recipe.Inputs.Length];
    OutputQueue = new int[Recipe.Outputs.Length];
  }

  void OnDestroy() => CraftTask?.Dispose();
}