using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Encounter : MonoBehaviour {
  public abstract Task Run(TaskScope scope);
}