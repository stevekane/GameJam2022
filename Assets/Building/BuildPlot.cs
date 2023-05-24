using UnityEngine;

[RequireComponent(typeof(Crafter))]
[RequireComponent(typeof(BuildObject))]
public class BuildPlot : MonoBehaviour {
  public void Craft(Recipe buildRecipe) {
    var crafter = GetComponent<Crafter>();
    crafter.CurrentRecipe = buildRecipe;
    crafter.RequestCraft();
  }
}