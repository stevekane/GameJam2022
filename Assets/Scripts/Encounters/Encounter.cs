using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct SpawnRequest {
  public Transform transform;
  public SpawnConfig config;
}

public abstract class Encounter : MonoBehaviour {
  public abstract Task Run(TaskScope scope);
}