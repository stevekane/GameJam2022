using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  public class AimAndFire : AbstractActionBehavior {
    [SerializeField] CharacterController Controller;
    [SerializeField] ActionManager ActionManager;
    [SerializeField] Aiming Aiming;
    [SerializeField] Velocity Velocity;
    [SerializeField] Fire Fire;
    [SerializeField] Aim Aim;
    [SerializeField] Timeval Duration = Timeval.FromSeconds(2);
    public override bool CanStart() => Controller.isGrounded && !Aiming.Value;
    public override void OnStart() => ActionManager.Scope.Run(Run);
    public async Task Run(TaskScope scope) {
      try {
        Aiming.Value = true;
        Velocity.Value.x = 0;
        Velocity.Value.z = 0;
        await scope.Ticks(Duration.Ticks);
      } finally {
        Aiming.Value = false;
      }
    }
  }
}