using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BuildObject : MonoBehaviour {
  public string Name;
  public Vector2Int Size;
  public bool CanPlaceMultiple = false;
  public BuildPlot BuildPlot;
  public Recipe BuildRecipe;
  public AssetReference Asset;

  public BuildObject Construct(Vector3 position, Quaternion rotation) {
    if (BuildPlot && BuildRecipe) {
      var plot = Instantiate(BuildPlot, position, rotation);
      plot.BuildRecipe = BuildRecipe;
      return plot.GetComponent<BuildObject>();
    }
    return Instantiate(this, position, rotation);
  }

  void Start() {
    SetName();
    BuildObjectManager.Instance.OnBuildObjectCreated(this);
    ItemFlowManager.Instance.OnBuildingsChanged();
  }
  void OnDestroy() {
    BuildObjectManager.Instance.OnBuildObjectDestroyed(this);
    ItemFlowManager.Instance.OnBuildingsChanged();
  }
#if UNITY_EDITOR
  void SetName() {
    // Generate a unique name for debugging purposes.
    var prefix = $"{Asset.editorAsset.name}_";
    var existing = FindObjectsOfType<BuildObject>().Where(bo => bo.gameObject.name.StartsWith(prefix));
    var id = existing.ToArray().Length + 1;
    gameObject.name = $"{prefix}{id}";
  }

  [ContextMenu("Update AssetReference for prefab")]
  void UpdateAssetReference() {
    var assetPath = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.assetPath;
    if (assetPath != null && assetPath.Length > 0) {
      var guid = AssetDatabase.AssetPathToGUID(assetPath);
      if (Asset.AssetGUID != guid) {
        Debug.Log($"Updating Asset path to {assetPath}");
        Asset = new(guid);
      }
    }
  }
#else
  void SetName() { }
#endif
}