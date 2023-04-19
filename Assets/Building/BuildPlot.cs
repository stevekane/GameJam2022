using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Crafter))]
[RequireComponent(typeof(BuildObject))]
public class BuildPlot : MonoBehaviour {
  // TODO: This doesn't work after loading. Need to store the recipe somewhere.
  public Recipe BuildRecipe { get; set; }
  public void TryConstruct() {
    var crafter = GetComponent<Crafter>();
    Debug.Assert(crafter.Recipes.Contains(BuildRecipe));
    ItemFlowManager.Instance.AddCraftRequest(crafter, BuildRecipe);
  }
}