using System.Linq;
using UnityEngine;

public class BuildObject : MonoBehaviour {
  public Vector2Int Size;
  public bool CanPlaceMultiple = false;
  public BuildPlot BuildPlot;
  public Recipe BuildRecipe;

  public BuildObject Construct(Vector3 position, Quaternion rotation) {
    if (BuildPlot && BuildRecipe) {
      var plot = Instantiate(BuildPlot, position, rotation);
      plot.Craft(BuildRecipe);
      return plot.GetComponent<BuildObject>();
    }
    return Instantiate(this, position, rotation);
  }

  void Start() {
    SetName();
  }
  void SetName() {
#if UNITY_EDITOR
    // Generate a unique name for debugging purposes.
    var prefix = $"{GetComponent<SaveObject>().Asset.editorAsset.name}_";
    var existing = FindObjectsOfType<BuildObject>().Where(bo => bo.gameObject.name.StartsWith(prefix));
    var id = existing.ToArray().Length + 1;
    gameObject.name = $"{prefix}{id}";
#endif
  }
}