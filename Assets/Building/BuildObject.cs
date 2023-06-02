using ES3Internal;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.AI.Navigation;
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

  void Awake() {
    SetName();
  }
  void Start() {
    FindObjectOfType<NavMeshSurface>().BuildNavMesh();
  }
  void SetName() {
#if UNITY_EDITOR
    // Generate a unique name for debugging purposes.
    var prefix = Regex.Replace(name, "\\([^)]*\\)", "");
    prefix = prefix.TrimEnd(' ');
    var existing = FindObjectsOfType<BuildObject>().Where(bo => bo.gameObject.name.StartsWith(prefix));
    var id = existing.ToArray().Length + 1;
    gameObject.name = $"{prefix}_{id}";
#endif
  }
}