using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  [Serializable]
  public class FireAction : AbstractAction {
    [field:SerializeField] public override string Name { get; set; }
    [field:SerializeField] public override ButtonCode ButtonCode { get; set; }
    [field:SerializeField] public override ButtonPressType ButtonPressType { get; set; }
    [SerializeField] Transform Origin;
    [SerializeField] GameObject BulletPrefab;
    public override bool CanStart() => true;
    public override void OnStart() {
      var bullet = GameObject.Instantiate(BulletPrefab, Origin.position, Origin.rotation);
      bullet.gameObject.SetActive(true);
      bullet.GetComponent<Rigidbody>().AddForce(Origin.forward * 10, ForceMode.Impulse);
    }
  }

  [Serializable]
  public class AimAction : AbstractAxisAction {
    [field:SerializeField] public override string Name { get; set; }
    [field:SerializeField] public override AxisCode AxisCode { get; set; }
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] CharacterController Controller;
    public override bool CanStart() => true;
    public override void OnStart(AxisState axisState) {
      var direction = axisState.XZFrom(PersonalCamera.Current);
      if (direction.magnitude > 0)
        Controller.transform.forward = direction;
    }
  }

  public class AimAndFire : AbstractActionBehavior {
    [field:SerializeField] public override string Name { get; set; } = "Aim and Fire";
    [field:SerializeField] public override ButtonCode ButtonCode { get; set; }
    [field:SerializeField] public override ButtonPressType ButtonPressType { get; set; }
    [SerializeField] CharacterController Controller;
    [SerializeField] ActionManager ActionManager;
    [SerializeField] Aiming Aiming;
    [SerializeField] Velocity Velocity;
    [SerializeField] FireAction FireAction;
    [SerializeField] AimAction AimAction;
    public override bool CanStart() => Controller.isGrounded && !Aiming.Value;
    // TODO: This bothersome setup is needed because tasks start asyncronously currently
    public override void OnStart() {
      Aiming.Value = true;
      Velocity.Value.x = 0;
      Velocity.Value.z = 0;
      ActionManager.Scope.Start(Fire);
    }
    public async Task Fire(TaskScope scope) {
      try {
        ActionManager.Actions.Add(FireAction);
        ActionManager.AxisActions.Add(AimAction);
        await scope.Ticks(120);
      } finally {
        Aiming.Value = false;
        ActionManager.Actions.Remove(FireAction);
        ActionManager.AxisActions.Remove(AimAction);
      }
    }
  }
}