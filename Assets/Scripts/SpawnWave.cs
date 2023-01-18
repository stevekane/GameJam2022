using System.Threading.Tasks;
using UnityEngine;

public abstract class SpawnWave : MonoBehaviour {
  public abstract Task Spawn(TaskScope scope, int wave);
}