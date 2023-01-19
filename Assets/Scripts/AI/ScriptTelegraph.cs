using System.Threading.Tasks;
using UnityEngine;

public class ScriptTelegraph : ScriptTask {
  public enum TelegraphBehaviors { TelegraphThenAttack, TelegraphDuringAttack, DontTelegraph };
  [SerializeField] TelegraphBehaviors TelegraphBehavior;

  public override async Task Run(TaskScope scope, Transform self, Transform target) {
    var flash = self.GetComponent<Flash>();
    TaskFunc telegraph = TelegraphBehavior switch {
      TelegraphBehaviors.TelegraphThenAttack => async s => await flash.RunStrobe(s, Color.red, Timeval.FromMillis(150), 3),
      TelegraphBehaviors.TelegraphDuringAttack => async s => { _ = flash.RunStrobe(s, Color.red, Timeval.FromMillis(150), 3); await scope.Yield(); },
      TelegraphBehaviors.DontTelegraph => async s => await s.Yield(),
      _ => null,
    };
    await telegraph(scope);
  }
}
