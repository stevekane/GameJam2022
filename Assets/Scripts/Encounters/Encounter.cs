using System;
using System.Collections;
using UnityEngine;

[Serializable]
public struct SpawnRequest {
  public Transform transform;
  public SpawnConfig config;
}

public abstract class Encounter : MonoBehaviour {
  public Bundle Bundle;
  public abstract IEnumerator Run();
}