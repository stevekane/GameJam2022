using UnityEngine;

public class WorldSpaceMessageManager : MonoBehaviour {
  public static WorldSpaceMessageManager Instance;

  [SerializeField] WorldSpaceMessage Prefab;

  void Awake() {
    Instance = this;
  }

  void OnDestroy() {
    Instance = null;
  }

  public WorldSpaceMessage SpawnMessage(string message, Vector3 position) {
    var worldSpaceMessage = Instantiate(Prefab, position, Quaternion.identity, transform);
    worldSpaceMessage.SetMessage(message);
    return worldSpaceMessage;
  }
}
