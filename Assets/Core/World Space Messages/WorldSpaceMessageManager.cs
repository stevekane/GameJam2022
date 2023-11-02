using UnityEngine;

public class WorldSpaceMessageManager : SingletonBehavior<WorldSpaceMessageManager> {
  [SerializeField] WorldSpaceMessage Prefab;

  public WorldSpaceMessage SpawnMessage(string message, Vector3 position, float lifetime = -1f) {
    var worldSpaceMessage = Instantiate(Prefab, position, Quaternion.identity, transform);
    worldSpaceMessage.Message = message;
    if (lifetime > 0f)
      Destroy(worldSpaceMessage.gameObject, lifetime);
    return worldSpaceMessage;
  }

  public WorldSpaceMessage SpawnMessage(WorldSpaceMessage prefab, string message, Vector3 position, float lifetime = -1f) {
    var worldSpaceMessage = Instantiate(prefab, position, Quaternion.identity, transform);
    worldSpaceMessage.Message = message;
    if (lifetime > 0f)
      Destroy(worldSpaceMessage.gameObject, lifetime);
    return worldSpaceMessage;
  }
}