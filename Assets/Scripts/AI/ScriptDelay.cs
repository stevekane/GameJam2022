using System.Threading.Tasks;
using UnityEngine;

public class ScriptDelay : ScriptTask {
  public Timeval Delay;
  public override Task Run(TaskScope scope, Transform self, Transform target) => scope.Delay(Delay);
}
