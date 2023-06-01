using ES3Types;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SaveObject : MonoBehaviour, ISaveableObject {
  public AssetReference Asset;
  List<ISaveableComponent> Saveables = new();

  public interface ISaveableComponent {
    public ILoadableComponent Save();
  }
  public interface ILoadableComponent {
    public void Load(GameObject obj);
  }
  public ILoadableObject Save() {
    if (Asset.Asset == null) {
      Asset.LoadAssetAsync<GameObject>().WaitForCompletion();
    }
    return new Serialized {
      AssetGUID = Asset.AssetGUID,
      Position = transform.position,
      Rotation = transform.rotation,
      //Components = Saveables.ToArray()
      Components = Saveables.Select(c => c.Save()).ToArray()
    };
  }
  class Serialized : ILoadableObject {
    public string AssetGUID;
    public Vector3 Position;
    public Quaternion Rotation;
    public ILoadableComponent[] Components;
    public void Load() {
      var asset = new AssetReference(AssetGUID);
      asset.LoadAssetAsync<GameObject>().WaitForCompletion();
      //var go = await Asset.InstantiateAsync(Position, Rotation).Task;
      var go = asset.InstantiateAsync(Position, Rotation).WaitForCompletion();
      Components.ForEach(o => o.Load(go));
    }
  }

  public void RegisterSaveable(ISaveableComponent component) {
    Saveables.Add(component);
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