using System.Collections.Generic;
using UnityEngine;

public static class Volatile {
  public static Dictionary<int, GameObject> References = new Dictionary<int, GameObject>();

  public static int Count { get => References.Count; }

  public readonly struct Handle {
    public readonly int ID;
    public Handle(GameObject gameObject) {
      ID = gameObject.GetInstanceID();
      if (!References.TryGetValue(ID, out GameObject n)) {
        References.Add(ID, gameObject);
      }
    }
  }

  public readonly struct Dereference {
    public readonly GameObject GameObject;
    public Dereference(Handle handle) {
      if (References.TryGetValue(handle.ID, out GameObject m)) {
        if (m) {
          GameObject = m;
        } else {
          References.Remove(handle.ID);
          GameObject = null;
        }
      } else {
        GameObject = null;
      }
    }
  }
}