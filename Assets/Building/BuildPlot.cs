using System;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using static SaveObject;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(Crafter))]
[RequireComponent(typeof(BuildObject))]
public class BuildPlot : MonoBehaviour, ISaveableComponent {
  public Recipe BuildRecipe { get; set; }

  SaveObject SaveObject;
  public ILoadableComponent Save() => new Serialized { BuildRecipe = BuildRecipe };
  class Serialized : ILoadableComponent {
    public Recipe BuildRecipe;
    public void Load(GameObject go) {
      go.GetComponent<BuildPlot>().BuildRecipe = BuildRecipe;
    }
  }

  void Start() {
    this.InitComponent(out SaveObject);
    SaveObject.RegisterSaveable(this);

    var crafter = GetComponent<Crafter>();
    Debug.Assert(BuildRecipe && crafter.Recipes.Contains(BuildRecipe));
    ItemFlowManager.Instance.AddCraftRequest(crafter, BuildRecipe);
  }
}