using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Crafter))]
[RequireComponent(typeof(BuildObject))]
public class BuildPlot : MonoBehaviour {
  public Recipe BuildRecipe { get; set; }

  public void TryConstruct() {
    var crafter = GetComponent<Crafter>();
    Debug.Assert(crafter.Recipes.Contains(BuildRecipe));
    ItemFlowManager.Instance.AddCraftRequest(crafter, BuildRecipe);
  }
}