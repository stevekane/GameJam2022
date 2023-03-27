using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BuildObject : MonoBehaviour {
  public string Name;
  public Vector2Int Size;
  public bool CanPlaceMultiple = false;
  public AssetReference Asset;

  class Serialized : ISerializeableAsset {
    public AssetReference Asset;
    public Vector3 Position;
    public Quaternion Rotation;
    public void Load() {
      Asset.InstantiateAsync(Position, Rotation);
    }
  }
  public ISerializeableAsset Save() {
    return new Serialized { Asset = Asset, Position = transform.position, Rotation = transform.rotation };
  }

#if UNITY_EDITOR
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
#endif
}