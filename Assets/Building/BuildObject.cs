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
}