using System.Threading.Tasks;
using UnityEngine;

public abstract class ScriptTask : MonoBehaviour {
  public abstract Task Run(TaskScope scope, Transform self, Transform target);
}
