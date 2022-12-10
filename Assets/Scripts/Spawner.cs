using System.Collections.Generic;
using UnityEngine;

public class DestroyNotifier : MonoBehaviour {
  public delegate void DestroyCallbackFunc(GameObject obj);
  public DestroyCallbackFunc DestroyCallback;

  private void OnDestroy() {
    DestroyCallback?.Invoke(gameObject);
  }
}

public class Spawner : MonoBehaviour {
  public GameObject Spawn;
  public int MaxActiveSpawns = 1;
  public Timeval SpawnPeriod;

  List<GameObject> ActiveSpawns = new List<GameObject>();
  int FramesRemaining = 0;

  void Awake() {
    FramesRemaining = SpawnPeriod.Ticks;
  }

  void FixedUpdate() {
    if (ActiveSpawns.Count < MaxActiveSpawns && --FramesRemaining <= 0) {
      var spawn = Instantiate(Spawn);
      spawn.SetActive(true);
      spawn.transform.SetParent(transform, false);
      spawn.transform.SetPositionAndRotation(transform.position, transform.rotation);
      ActiveSpawns.Add(spawn);
      spawn.AddComponent<DestroyNotifier>().DestroyCallback = ObjectDestroyed;
      FramesRemaining = SpawnPeriod.Ticks;
    }
  }

  void ObjectDestroyed(GameObject obj) {
    ActiveSpawns.Remove(obj);
  }
}