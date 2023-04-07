using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A set of connected capillaries, and the crafters that are connected to it.
public class CapillaryGroup {
  public int Index;  // Index into the list, also used as a UID.
  public HashSet<Vector2Int> Cells = new();
  public List<Crafter> Producers = new();
  public List<Crafter> Consumers = new();
  public override string ToString() => $"Group{Cells.First()}";
}

public class BuildObjectManager : MonoBehaviour {
  public static BuildObjectManager Instance;

  Crafter[] Crafters;
  List<CapillaryGroup> CapillaryGroups;
  bool Dirty = true;

  public void OnBuildObjectCreated(BuildObject obj) {
    Dirty = true;
  }
  public void OnBuildObjectDestroyed(BuildObject obj) {
    Dirty = true;
  }
  public void MaybeRefresh() {
    if (Dirty) {
      Refresh();
      Dirty = false;
    }
  }

  void Refresh() {
    CapillaryGroups = new();
    Crafters = FindObjectsOfType<Crafter>();
    foreach (var crafter in Crafters) {
      var inGroup = GetOrCreateCapillaryGroup(crafter.InputPortCell, crafter.transform.position.y);
      inGroup.Consumers.Add(crafter);
      crafter.InputCapillaryGroup = inGroup;
      var outGroup = GetOrCreateCapillaryGroup(crafter.OutputPortCell, crafter.transform.position.y);
      outGroup.Producers.Add(crafter);
      crafter.OutputCapillaryGroup = outGroup;
    }
  }

  public CapillaryGroup GetCapillaryGroup(Vector2Int cell) => CapillaryGroups.Find(g => g.Cells.Contains(cell));
  CapillaryGroup GetOrCreateCapillaryGroup(Vector2Int cell, float y) => GetCapillaryGroup(cell) ?? CreateCapillaryGroup(cell, y);
  CapillaryGroup CreateCapillaryGroup(Vector2Int start, float y) {
    var group = new CapillaryGroup { Index = CapillaryGroups.Count };
    CapillaryGroups.Add(group);
    var toVisit = new Queue<Vector2Int>();
    toVisit.Enqueue(start);
    while (toVisit.TryDequeue(out var visit)) {
      if (group.Cells.Contains(visit)) continue;
      var obj = BuildGrid.GetCellContents(visit, y);
      if (obj && obj.GetComponent<Capillary>()) {
        group.Cells.Add(visit);
        toVisit.Enqueue(visit + Vector2Int.left);
        toVisit.Enqueue(visit + Vector2Int.right);
        toVisit.Enqueue(visit + Vector2Int.up);
        toVisit.Enqueue(visit + Vector2Int.down);
      }
    }
    // Allow groups with no Capillaries, so we can output to the ground.
    if (group.Cells.Count == 0)
      group.Cells.Add(start);
    return group;
  }
}