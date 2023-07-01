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

  public WorldSpaceMessage SpawnMessage(string message, Vector3 position, float lifetime = -1f) {
    var worldSpaceMessage = Instantiate(Prefab, position, Quaternion.identity, transform);
    worldSpaceMessage.Message = message;
    if (lifetime > 0f)
      Destroy(worldSpaceMessage, lifetime);
    return worldSpaceMessage;
  }
}
