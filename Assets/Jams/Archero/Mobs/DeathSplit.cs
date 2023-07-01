using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  // Note: This is used for both the splitter and the splittee. Splittees that don't split
  // futrther can just have an empty SplitInto.
  public class DeathSplit : MonoBehaviour {
    public DeathSplit[] SplitInto;
    public Vector3 SplitVelocity;
    public int SplitIndex { get; private set; } = 0;
    public int Generation { get; private set; } = 0;

    float YRotation = 0;
    TaskScope Scope = new();

    public void OnDeath() {
      var rotationIncrements = 360f / SplitInto.Length;
      for (var i = 0; i < SplitInto.Length; i++) {
        var child = Instantiate(SplitInto[i], transform.position, transform.rotation);
        child.SplitIndex = i;
        child.Generation = Generation+1;
        child.YRotation = i * rotationIncrements;
      }
    }

    void Start() {
      if (Generation > 0) {
        SplitVelocity = Quaternion.Euler(0, YRotation, 0) * SplitVelocity;
        Scope.Start(SplitMove);
      }
    }
    void OnDestroy() => Scope.Dispose();

    async Task SplitMove(TaskScope scope) {
      Debug.Log($"Splitting with velocity {SplitVelocity}");
      AI ai = GetComponent<AI>();
      await scope.Any(
        Waiter.Seconds(.3f),
        Waiter.Repeat(() => ai.ScriptedVelocity = SplitVelocity));
    }
  }
}