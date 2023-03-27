using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.Progress;

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
  Dictionary<Machine, int> PlayerCraftRequests = new();
  Dictionary<Edge, int> EdgeDemands = new();

  public void AddCraftRequest(Machine producer) {
    PlayerCraftRequests[producer] = PlayerCraftRequests.GetValueOrDefault(producer) + 1;
    BuildGraph(); // TODO: cache this
    SolveForRequests();
  }

  struct Edge {
    public Machine From;
    public int OutputIdx;
    public Machine To;
    public int InputIdx;

    public bool IsNull() => From == null;

    public override int GetHashCode() => (From, To, OutputIdx, InputIdx).GetHashCode();
    public override bool Equals(object other) => other is Edge e && (From, To, OutputIdx, InputIdx).Equals((e.From, e.To, e.OutputIdx, e.InputIdx));
    public override string ToString() => (From, OutputIdx, To, InputIdx).ToString();
  }
  List<Edge> Edges;
  List<Machine> Machines;
  Dictionary<Edge, Vector2Int> EdgeOutputCells = new();

  void BuildGraph() {
    Edges = new();
    Machines = FindObjectsOfType<Machine>().ToList();
    foreach (var machine in Machines) {
      var connected = machine.FindConnectedMachines();
      for (int i = 0; i < machine.Recipe.Inputs.Length; i++) {
        var input = machine.Recipe.Inputs[i];
        foreach ((var provider, var outputCell) in connected) {
          int outputIdx = Array.FindIndex(provider.Recipe.Outputs, output => output.Item == input.Item);
          if (outputIdx != -1) {
            var edge = new Edge { From = provider, OutputIdx = outputIdx, To = machine, InputIdx = i };
            if (!Edges.Contains(edge)) {
              Edges.Add(edge);
              EdgeOutputCells[edge] = outputCell;
            }
          }
        }
      }
    }
  }

  // TODO: clean this up ffs
  void SolveForRequests() {
    var edgeAmounts = new Dictionary<Edge, Decision>();
    var inputEdges = new Dictionary<(Machine, int), List<Edge>>();
    var outputEdges = new Dictionary<(Machine, int), List<Edge>>();

    var solver = SolverContext.GetContext();
    solver.ClearModel();
    var model = solver.CreateModel();

    // Main goal: minimize time spent crafting.
    var t = new Decision(Domain.Real, "t");
    model.AddDecision(t);
    model.AddGoal("Goal", GoalKind.Minimize, t);
    model.AddConstraint(null, t >= 0);

    string DecisionName(object obj) => obj.ToString().Replace(' ', '_').Replace(',', '_').Replace('(', '_').Replace(')', '_');
    // Adds constraint on the maximum that can consumed/produced in the given time.
    Decision AddMaxAmountConstraint(Machine machine, Recipe.ItemAmount amount) {
      var decidedAmount = new Decision(Domain.Real, $"max_{DecisionName(machine.name)}_{DecisionName(amount.Item.name)}");
      var rate = amount.Count / machine.Recipe.CraftTime;
      model.AddDecision(decidedAmount);
      model.AddConstraint(null, decidedAmount <= rate * t);
      return decidedAmount;
    }
    // Adds constraints on the recipe ratios by ensuring time to consume/produce each input/output is the same.
    void AddRecipeRatioConstraints(Machine machine, Decision[] inputAmounts, Decision[] outputAmounts) {
      var recipe = machine.Recipe;
      Term TimeToCraft(int idx, Decision[] decidedAmounts, Recipe.ItemAmount[] amounts) =>
        decidedAmounts[idx] / (amounts[idx].Count / recipe.CraftTime);
      var referenceTime =
        inputAmounts.Length > 0 ? TimeToCraft(0, inputAmounts, recipe.Inputs) :
        outputAmounts.Length > 0 ? TimeToCraft(0, outputAmounts, recipe.Outputs) : null;
      for (var i = 0; i < inputAmounts.Length; i++)
        model.AddConstraint(null, TimeToCraft(i, inputAmounts, recipe.Inputs) == referenceTime);
      for (var i = 0; i < outputAmounts.Length; i++)
        model.AddConstraint(null, TimeToCraft(i, outputAmounts, recipe.Outputs) == referenceTime);
    }
    var empty = new List<Edge>();
    // Adds constraint: sum(edges attached to machine slot N) = machine's slot_N decision.
    void AddEdgeToMachineConstraints(Machine machine, Dictionary<(Machine, int), List<Edge>> edgeMap, Recipe.ItemAmount[] slots, Decision[] decidedAmounts) {
      for (var i = 0; i < slots.Length; i++) {
        var edges = edgeMap.GetValueOrDefault((machine, i), empty);
        if (edges.Count == 0) continue; // TODO: figure out what to do about a disconnected input, and output
        var edgeSum = edges.Aggregate((Term)0, (term, e) => term + edgeAmounts[e]);
        model.AddConstraint(null, edgeSum == decidedAmounts[i]);
      }
    }

    // Add decision variables for each edge.
    foreach (var edge in Edges) {
      model.AddDecision(edgeAmounts[edge] = new Decision(Domain.Real, $"e_{DecisionName(edge)}"));
      outputEdges.GetOrAdd((edge.From, edge.OutputIdx), () => new()).Add(edge);
      inputEdges.GetOrAdd((edge.To, edge.InputIdx), () => new()).Add(edge);
    }

    // Set up the constraints for each machine's input and output, and collect the decided output amounts.
    Dictionary<Machine, Decision[]> machineOutputDemands = new();
    foreach (var machine in Machines) {
      var inputAmounts = machine.Recipe.Inputs.Select(amount => AddMaxAmountConstraint(machine, amount)).ToArray();
      var outputAmounts = machine.Recipe.Outputs.Select(amount => AddMaxAmountConstraint(machine, amount)).ToArray();
      AddRecipeRatioConstraints(machine, inputAmounts, outputAmounts);
      AddEdgeToMachineConstraints(machine, inputEdges, machine.Recipe.Inputs, inputAmounts);
      AddEdgeToMachineConstraints(machine, outputEdges, machine.Recipe.Outputs, outputAmounts);

      if (PlayerCraftRequests.TryGetValue(machine, out var numCrafts)) {
        // Only need to constrain one output to ensure the machine will craft it.
        if (machine.Recipe.Outputs.Length > 0) {
          var outputsRequested = machine.Recipe.Outputs[0].Count * numCrafts;
          model.AddConstraint(null, outputAmounts[0] >= outputsRequested);
        } else {
          var inputsRequested = machine.Recipe.Inputs[0].Count * numCrafts;
          model.AddConstraint(null, inputAmounts[0] >= inputsRequested);
        }
      }
      machineOutputDemands[machine] = outputAmounts;
    }

    var solution = solver.Solve(new Directive() { TimeLimit = 5000 });

    //foreach (var c in model.Constraints) {
    //  Debug.Log($"constraint: {c.Expression}");
    //}
    //Debug.Log($"Result time={t}");

    // Figure out how many crafts each machine needs to execute.
    foreach (var machine in Machines) {
      var numCrafts = PlayerCraftRequests.GetValueOrDefault(machine, 0);
      var outputDemands = machineOutputDemands[machine];
      for (var i = 0; i < machine.Recipe.Outputs.Length; i++) {
        //Debug.Log($"Machine {machine} outputs {outputDemands[i].ToDouble()} {machine.Recipe.Outputs[i].Item}");
        if (outputDemands[i].ToDouble() > numCrafts)
          numCrafts = Mathf.CeilToInt((float)(outputDemands[i].ToDouble() / machine.Recipe.Outputs[i].Count));
      }
      machine.TryStartCrafting();
      //Debug.Log($"Machine crafts: {machine} crafts {numCrafts}");
    }
    foreach (var edge in Edges) {
      EdgeDemands[edge] = (int)edgeAmounts[edge].ToDouble();
    }
  }

  public void OnCraftFinished(Machine machine, int[] outputQueue) {
    if (PlayerCraftRequests.TryGetValue(machine, out var numCrafts) && numCrafts > 0)
      PlayerCraftRequests[machine] = numCrafts - 1;
    for (var i = 0; i < outputQueue.Length; i++) {
      var edgeDemand = EdgeDemands.FirstOrDefault(e => e.Key.From == machine && e.Key.OutputIdx == i);
      if (!edgeDemand.Key.IsNull())
        TryOutput(edgeDemand.Key);
    }
  }

  void TryOutput(Edge edge) {
    Debug.Assert(EdgeDemands[edge] > 0);

    var outputCell = EdgeOutputCells[edge];
    var item = edge.From.Recipe.Outputs[edge.OutputIdx].Item;
    if (IsCellOccupied(outputCell, item))
      return;
    if (!edge.From.TryOutputItem(edge.OutputIdx))
      return;

    var instance = item.Spawn(edge.From.transform.position);
    var flow = new ItemFlow {
      Object = instance,
      Path = ComputePath(outputCell, edge),
      Edge = edge,
    };
    AdvanceOnPath(flow);
    Items.Add(flow);

    if (--EdgeDemands[edge] <= 0)
      EdgeDemands.Remove(edge);
    edge.From.TryStartCrafting();
  }

  List<Vector2Int> ComputePath(Vector2Int startCell, Edge edge) {
    var destCell = startCell;
    var y = edge.From.transform.position.y;
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
      if (obj != gameObject && obj.TryGetComponent(out Machine machine) && machine == edge.To)
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
      item.Edge.To.InsertItem(item.Edge.InputIdx);
      item.Object.gameObject.Destroy();
    }
    toConsume.ForEach(f => Items.Remove(f));
  }

  void AdvanceOnPath(ItemFlow item) {
    if (IsCellOccupied(item.Path[0], item.Object.Info)) return;
    item.NextWorldPos = BuildGrid.GridToWorld(item.Path[0], item.Edge.From.transform.position.y);
    item.Path.RemoveAt(0);
  }

  void FixedUpdate() {
    MoveItems();
  }

#if true
  void Awake() { Test6();  }

  void Test() {
    var solver = SolverContext.GetContext();
    var model = solver.CreateModel();

    var e01 = new Decision(Domain.IntegerNonnegative, "e01");
    var e02 = new Decision(Domain.IntegerNonnegative, "e02");
    var e13 = new Decision(Domain.IntegerNonnegative, "e13");
    var e23 = new Decision(Domain.IntegerNonnegative, "e23");
    var e34 = new Decision(Domain.IntegerNonnegative, "e34");
    model.AddDecisions(new[] { e01, e02, e13, e23, e34 });

    model.AddGoal("Goal", GoalKind.Maximize, e34);

    var i = 0;
    void DoEdge(Decision e, int max) {
      model.AddConstraint(null, e >= 0);
      model.AddConstraint(null, e <= max);
    }
    DoEdge(e01, 10000);
    DoEdge(e02, 10000);
    DoEdge(e13, 1);
    DoEdge(e23, 1);
    DoEdge(e34, 2);

    var m1i = new Decision(Domain.IntegerNonnegative, "m1i");
    var m2i = new Decision(Domain.IntegerNonnegative, "m2i");
    var m3i = new Decision(Domain.IntegerNonnegative, "m3i");
    var m1o = new Decision(Domain.IntegerNonnegative, "m1o");
    var m2o = new Decision(Domain.IntegerNonnegative, "m2o");
    var m3o = new Decision(Domain.IntegerNonnegative, "m3o");
    model.AddDecisions(new[] { m1i, m2i, m3i, m1o, m2o, m3o });

    // constraints: edges on inputs
    model.AddConstraint(null, m1i == e01);
    model.AddConstraint(null, m2i == e02);
    model.AddConstraint(null, m3i == e13 + e23);
    // constraints: edges on outputs
    model.AddConstraint(null, m1o == e13);
    model.AddConstraint(null, m2o == e23);
    model.AddConstraint(null, m3o == e34);
    // constraints: input/output rates match
    model.AddConstraint(null, m1i / 2 == m1o / 1);
    model.AddConstraint(null, m2i / 2 == m2o / 1);
    model.AddConstraint(null, m3i / 2 == m3o / 1);

    var solution = solver.Solve();
    Debug.Log($"Test: e01={e01} e02={e02} e13={e13} e23={e23} output = {e34.GetDouble()} sol={solution}");

    solver.ClearModel();
  }

  async Task Test2() {
    var solver = SolverContext.GetContext();
    var model = solver.CreateModel();

    var m1i = new Decision(Domain.IntegerNonnegative, null);
    var m2i = new Decision(Domain.IntegerNonnegative, null);
    var m3i = new Decision(Domain.IntegerNonnegative, null);
    var m1o = new Decision(Domain.IntegerNonnegative, null);
    var m2o = new Decision(Domain.IntegerNonnegative, null);
    var m3o = new Decision(Domain.IntegerNonnegative, null);
    var t = new Decision(Domain.Real, null);
    model.AddDecisions(new[] { m1i, m2i, m3i, m1o, m2o, m3o, t });
    model.AddGoal("Goal", GoalKind.Minimize, t);

    model.AddConstraint(null, t >= 0);
    // constraints: connections
    model.AddConstraint(null, m1i == 2 * t);
    model.AddConstraint(null, m2i == 2 * t);
    model.AddConstraint(null, m3i == m1o + m2o);
    // constraints: machine input and output rates
    model.AddConstraint(null, m1i <= 2 * t);
    model.AddConstraint(null, m1o <= 1 * t);
    model.AddConstraint(null, m2i <= 2 * t);
    model.AddConstraint(null, m2o <= 1 * t);
    model.AddConstraint(null, m3i <= 2 * t);
    model.AddConstraint(null, m3o <= 1 * t);
    // constraints: input/output rates match
    model.AddConstraint(null, m1i / 2 == m1o / 1);
    model.AddConstraint(null, m2i / 2 == m2o / 1);
    model.AddConstraint(null, m3i / 2 == m3o / 1);
    model.AddConstraint(null, m1o == 1);

    var solution = solver.Solve();
    Debug.Log($"Test2 m1 = {m1i}->{m1o} m2 = {m2i}->{m2o} m3 = {m3i}->{m3o} sol={solution}");
    solver.ClearModel();
  }

  void Test3() {
    object a = new object();
    object b = new object();
    Dictionary<(object, object), int> map = new();
    map[(a, b)] = 1;
    map[(b, a)] = 2;
    map[(b, a)]++;
    Debug.Log($"map: {map[(a, b)]}, {map[(b, a)]}");
  }

  async Task Test4() {
    var solver = SolverContext.GetContext();
    var model = solver.CreateModel();

    var e01 = new Decision(Domain.IntegerNonnegative, "e01");
    var e02 = new Decision(Domain.IntegerNonnegative, "e02");
    var e13 = new Decision(Domain.IntegerNonnegative, "e13");
    var e23 = new Decision(Domain.IntegerNonnegative, "e23");
    var e34 = new Decision(Domain.IntegerNonnegative, "e34");
    var t = new Decision(Domain.Real, null);
    model.AddDecisions(new[] { e01, e02, e13, e23, e34, t });
    model.AddGoal("Goal", GoalKind.Minimize, t);

    var i = 0;
    void DoEdge(Decision e, int max) {
      //model.AddConstraint(null, e >= 0);
      //model.AddConstraint(null, e <= max);
    }
    DoEdge(e01, 10000);
    DoEdge(e02, 10000);
    DoEdge(e13, 1);
    DoEdge(e23, 1);
    DoEdge(e34, 2);

    var m1i = new Decision(Domain.IntegerNonnegative, "m1i");
    var m2i = new Decision(Domain.IntegerNonnegative, "m2i");
    var m3i = new Decision(Domain.IntegerNonnegative, "m3i");
    var m1o = new Decision(Domain.IntegerNonnegative, "m1o");
    var m2o = new Decision(Domain.IntegerNonnegative, "m2o");
    var m3o = new Decision(Domain.IntegerNonnegative, "m3o");
    model.AddDecisions(new[] { m1i, m2i, m3i, m1o, m2o, m3o });

    model.AddConstraint(null, t >= 0);
    // constraints: edges on inputs
    model.AddConstraint(null, m1i == e01);
    model.AddConstraint(null, m2i == e02);
    model.AddConstraint(null, m3i == e13 + e23);
    // constraints: edges on outputs
    model.AddConstraint(null, m1o == e13);
    model.AddConstraint(null, m2o == e23);
    model.AddConstraint(null, m3o == e34);
    // constraints: machine input and output rates
    model.AddConstraint(null, m1i <= 2 * t);
    model.AddConstraint(null, m1o <= 1 * t);
    model.AddConstraint(null, m2i <= 2 * t);
    model.AddConstraint(null, m2o <= 1 * t);
    model.AddConstraint(null, m3i <= 2 * t);
    model.AddConstraint(null, m3o <= 1 * t);
    // constraints: input/output rates match
    model.AddConstraint(null, m1i / 2 == m1o / 1);
    model.AddConstraint(null, m2i / 2 == m2o / 1);
    model.AddConstraint(null, m3i / 2 == m3o / 1);

    model.AddConstraint(null, e01 == 2*t);
    model.AddConstraint(null, e02 == 2*t);
    model.AddConstraint(null, e34 == 1);

    var solution = solver.Solve();
    Debug.Log($"Test4 m1 = {m1i}->{m1o} m2 = {m2i}->{m2o} m3 = {m3i}->{m3o} sol={solution}");

    solver.ClearModel();
  }

  async Task Test5() {
    var solver = SolverContext.GetContext();
    var model = solver.CreateModel();

    var eac = new Decision(Domain.Real, null);
    var ebc = new Decision(Domain.Real, null);
    var t = new Decision(Domain.Real, null);
    model.AddDecisions(new[] { eac, ebc, t });
    model.AddGoal("Goal", GoalKind.Minimize, t);

    var mao = new Decision(Domain.Real, null);
    var mbo = new Decision(Domain.Real, null);
    var mci1 = new Decision(Domain.Real, null);
    var mci2 = new Decision(Domain.Real, null);
    var mco = new Decision(Domain.Real, null);
    model.AddDecisions(new[] { mao, mbo, mci1, mci2, mco });

    model.AddConstraint(null, t >= 0);
    // constraints: edges on outputs
    model.AddConstraint(null, mao == eac);
    model.AddConstraint(null, mbo == ebc);
    // constraints: edges on inputs
    model.AddConstraint(null, mci1 == eac);
    model.AddConstraint(null, mci2 == ebc);
    // constraints: machine input and output rates
    model.AddConstraint(null, mao <= 1 * t);
    model.AddConstraint(null, mbo <= 1 * t);
    model.AddConstraint(null, mci1 <= 1 * t);
    model.AddConstraint(null, mci2 <= 1 * t);
    model.AddConstraint(null, mco <= 1 * t);
    // constraints: input/output rates match
    model.AddConstraint(null, mci2 / 1 == mci1 / 1);
    model.AddConstraint(null, mco / 1 == mci1 / 1);

    model.AddConstraint(null, mco >= 1);

    var solution = solver.Solve();
    Debug.Log($"Test5 ma = {mao} mb = {mbo} mc = {mci1}+{mci2}->{mco} sol={solution}");

    solver.ClearModel();
  }

  async Task Test6() {
    var solver = SolverContext.GetContext();
    solver.ClearModel();
    var model = solver.CreateModel();

    Decision NewD() { var d = new Decision(Domain.Real, null); model.AddDecision(d); return d; }
    var t = NewD();
    model.AddGoal("Goal", GoalKind.Minimize, t);

    (Decision, Decision) NewM() => (NewD(), NewD());
    var ma = new (Decision, Decision)[] {
      NewM(),
      NewM(),
      NewM(),
      NewM(),
    };
    var mb = new (Decision, Decision)[] {
      NewM(),
      NewM(),
    };
    var mc = NewM();

    var eab0 = new Decision[] { NewD(), NewD(), NewD() };
    var eab1 = NewD();
    var eb0c = NewD();
    var eb1c = NewD();

    var arange = new[] { 0, 1, 2, 3 };
    var brange = new[] { 0, 1 };
    model.AddConstraint(null, t >= 0);
    // constraints: edges on outputs
    model.AddConstraint(null, ma[0].Item2 == eab0[0]);
    model.AddConstraint(null, ma[1].Item2 == eab0[1]);
    model.AddConstraint(null, ma[2].Item2 == eab0[2]);
    model.AddConstraint(null, ma[3].Item2 == eab1);
    model.AddConstraint(null, mb[0].Item2 == eb0c);
    model.AddConstraint(null, mb[1].Item2 == eb1c);
    // constraints: edges on inputs
    model.AddConstraint(null, mb[0].Item1 == eab0[0] + eab0[1] + eab0[2]);
    model.AddConstraint(null, mb[1].Item1 == eab1);
    model.AddConstraint(null, mc.Item1 == eb0c + eb1c);
    // constraints: machine input and output rates
    arange.ForEach(i => { model.AddConstraint(null, ma[i].Item2 <= 1 * t); });
    brange.ForEach(i => { model.AddConstraint(null, mb[i].Item1 <= 3 * t); });
    brange.ForEach(i => { model.AddConstraint(null, mb[i].Item2 <= 1 * t); });
    model.AddConstraint(null, mc.Item1 <= 3 * t);
    model.AddConstraint(null, mc.Item2 <= 1 * t);
    // constraints: input/output rates match
    arange.ForEach(i => { model.AddConstraint(null, ma[i].Item1 / 1 == ma[i].Item2 / 1); });
    brange.ForEach(i => { model.AddConstraint(null, mb[i].Item1 / 3 == mb[i].Item2 / 1); });
    model.AddConstraint(null, mc.Item1 / 3 == mc.Item2 / 1);

    model.AddConstraint(null, ma[2].Item1 == 0);
    model.AddConstraint(null, mc.Item2 >= 2);

    var solution = solver.Solve();
    Debug.Log($"Test6 t={t} ma = {ma[0].Item2}, {ma[1].Item2}, {ma[2].Item2} -> {mb[0].Item1}; ma4 = {ma[3].Item2} -> {mb[1].Item1}; mb = {mb[0].Item2} + {mb[1].Item2} = {mc.Item1}; mco = {mc.Item2}");

    solver.ClearModel();
  }
#endif

}
